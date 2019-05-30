using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Assets.Scripts.Network
{
    //High performance UDP client/server (IPv4 Only)
    //Need minor refactor
    public sealed class UdpNetwork : IDisposable
    {
        public const int Mtu = 1400; //MAX SUPPORT VALUE IS 2047 !!! (11 bit data size in header)

        // Header bits description (8 bytes):
        // FR000DDD DDDDDDDD AAAAAAAA AAAAAAAA AAAAAAAA AAAAAAAA PPPPPPPP PPPPPPPP
        // F - fill flag
        // R - reset cursor flag
        // 0 - unused
        // D - data length
        // A - address (IPv4)
        // P - port

        private const int InternalHeaderSize = 8;

        private const int HeaderDataLengthMask = 2047;

        private const int HeaderFillFlagBitIndex = 7;
        private const int HeaderResetCursorFlagBitIndex = 6;

        private readonly Socket socket;

        private readonly SocketAsyncEventArgs sendArgs;
        private readonly SocketAsyncEventArgs receiveArgs;

        public SocketError SendError { get; private set; } = SocketError.Success;
        public SocketError ReceiveError { get; private set; } = SocketError.Success;

        private readonly byte[] sharedSendBuffer;
        private readonly byte[] sharedReceiveBuffer;

        public readonly IPEndPoint lastWriteRemoteIpEndPoint = new IPEndPoint(0, 0);
        public readonly IPEndPoint lastReadRemoteIpEndPoint = new IPEndPoint(0, 0);

        private int sendBufferWritePos;
        private int sendBufferCheckPos;
        private int sendBufferReadPos;

        private int receiveBufferWritePos;
        private int receiveBufferCheckPos;
        private int receiveBufferReadPos;

        private int sendHeaderFillCount;
        private int receiveHeaderFillCount;

        public UdpNetwork(int sendBufferSize, int receiveBufferSize)
        {
            if (sendBufferSize <= 0) throw new ArgumentException("Invalid send buffer size!");
            sharedSendBuffer = new byte[sendBufferSize + InternalHeaderSize + InternalHeaderSize];

            if (receiveBufferSize <= 0) throw new ArgumentException("Invalid receive buffer size!");
            sharedReceiveBuffer = new byte[receiveBufferSize + InternalHeaderSize + InternalHeaderSize];

            receiveBufferCheckPos = sharedReceiveBuffer.Length;
            sendBufferCheckPos = sharedSendBuffer.Length;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Disable virtual channel notifications. Control code: SIO_UDP_CONNRESET
            socket.IOControl(-1744830452, new byte[] { 0 }, new byte[] { 0 });

            sendArgs = new SocketAsyncEventArgs();
            sendArgs.SetBuffer(new byte[Mtu], 0, Mtu);
            sendArgs.RemoteEndPoint = new IPEndPoint(0, 0);
            sendArgs.Completed += ProcessSend;

            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.SetBuffer(new byte[Mtu], 0, Mtu);
            receiveArgs.RemoteEndPoint = new IPEndPoint(0, 0);
            receiveArgs.Completed += ProcessReceive;
        }

        public void InitAsServer(ushort port)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            socket.Bind(new IPEndPoint(IPAddress.Any, port));

            StartReceive();
        }

        public void InitAsClient()
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            StartReceive();
        }

        public void ApplyEndPointToWriter(IPEndPoint otherIpEndPoint)
        {
#pragma warning disable 618
            lastWriteRemoteIpEndPoint.Address.Address = otherIpEndPoint.Address.Address;
#pragma warning restore 618
            lastWriteRemoteIpEndPoint.Port = otherIpEndPoint.Port;
        }

        public void SendToLastClient(byte[] data, int offset, int length)
        {
#pragma warning disable 618
            lastWriteRemoteIpEndPoint.Address.Address = lastReadRemoteIpEndPoint.Address.Address;
#pragma warning restore 618
            lastWriteRemoteIpEndPoint.Port = lastReadRemoteIpEndPoint.Port;

            Send(data, offset, length);
        }
        public void Send(byte[] data, int offset, int length)
        {
            if (length <= 0 || length > sharedSendBuffer.Length + InternalHeaderSize + InternalHeaderSize) throw new ArgumentException("Invalid length!");
            if (offset + length > data.Length) throw new ArgumentException("Invalid args: offset + length > data array upper bound!");

            var addedResetCursor = false;

            TryAddPacketData:

            var nextIndexAfterCurrentPacketData = sendBufferWritePos + InternalHeaderSize + length;

            var needResetCursor = nextIndexAfterCurrentPacketData + InternalHeaderSize >= sharedSendBuffer.Length;

            var dataLengthToPack = needResetCursor ? InternalHeaderSize : InternalHeaderSize + length;

            var dataLengthToCheck = needResetCursor ? sharedSendBuffer.Length : dataLengthToPack;

            byte firstHeaderByte;

            while (sendBufferWritePos + dataLengthToCheck > sendBufferCheckPos && sendBufferCheckPos < sharedSendBuffer.Length)
            {
                firstHeaderByte = sharedSendBuffer[sendBufferCheckPos];

                if ((firstHeaderByte & (1 << HeaderFillFlagBitIndex)) != 0)
                {
                    Debug.LogError("UDP outgoing packet software drop! Send buffer part is not ready!");

                    if (addedResetCursor)
                    {
                        if (Interlocked.Increment(ref sendHeaderFillCount) > 1) return;

                        ContinueSend();
                    }

                    return;
                }

                if ((firstHeaderByte & (1 << HeaderResetCursorFlagBitIndex)) != 0)
                {
                    sendBufferCheckPos = sharedSendBuffer.Length;

                    break;
                }

                var oldDataSize = (firstHeaderByte << 8) | sharedSendBuffer[sendBufferCheckPos + 1];
                oldDataSize &= HeaderDataLengthMask;

                if (oldDataSize == 0)
                {
                    throw new Exception("Invalid send behavior!");
                }

                sendBufferCheckPos += InternalHeaderSize + oldDataSize;
            }

            if (needResetCursor)
            {
                firstHeaderByte = (1 << HeaderFillFlagBitIndex) | (1 << HeaderResetCursorFlagBitIndex);

                sharedSendBuffer[sendBufferWritePos] = firstHeaderByte;

                addedResetCursor = true;

                sendBufferWritePos = 0;

                sendBufferCheckPos = 0;

                goto TryAddPacketData;
            }

            Buffer.BlockCopy(data, offset, sharedSendBuffer, sendBufferWritePos + InternalHeaderSize, length);

            firstHeaderByte = (byte) ((1 << HeaderFillFlagBitIndex) | (length >> 8));
            var secondHeaderByte = (byte) length;

            sharedSendBuffer[sendBufferWritePos + 1] = secondHeaderByte;

#pragma warning disable 618
            var remoteIp = (uint)lastWriteRemoteIpEndPoint.Address.Address;
#pragma warning restore 618
            var remotePort = (ushort)lastWriteRemoteIpEndPoint.Port;

            sharedSendBuffer[sendBufferWritePos + 2] = (byte) remoteIp;
            sharedSendBuffer[sendBufferWritePos + 3] = (byte) (remoteIp >> 8);
            sharedSendBuffer[sendBufferWritePos + 4] = (byte) (remoteIp >> 16);
            sharedSendBuffer[sendBufferWritePos + 5] = (byte) (remoteIp >> 24);

            sharedSendBuffer[sendBufferWritePos + 6] = (byte) remotePort;
            sharedSendBuffer[sendBufferWritePos + 7] = (byte) (remotePort >> 8);

            Thread.MemoryBarrier();

            sharedSendBuffer[sendBufferWritePos] = firstHeaderByte;

            sendBufferWritePos += dataLengthToPack;

            Thread.MemoryBarrier();

            if(addedResetCursor)
            {
                if (Interlocked.Add(ref sendHeaderFillCount, 2) > 2) return;
            }
            else if (Interlocked.Increment(ref sendHeaderFillCount) > 1) return;

            ContinueSend();
        }

        private void ContinueSend()
        {
            ContinueSend:

            var firstHeaderByte = sharedSendBuffer[sendBufferReadPos];

            if ((firstHeaderByte & (1 << HeaderFillFlagBitIndex)) == 0)
            {
                Debug.LogError("INVALID CONTINUE SEND BEHAVIOUR");

                return;
            }

            if ((firstHeaderByte & (1 << HeaderResetCursorFlagBitIndex)) != 0)
            {
                sharedSendBuffer[sendBufferReadPos] = ResetFillFlag(firstHeaderByte);

                sendBufferReadPos = 0;

                Thread.MemoryBarrier();

                if (Interlocked.Decrement(ref sendHeaderFillCount) == 0) return;

                goto ContinueSend;
            }

            Thread.MemoryBarrier();

            var secondHeaderByte = sharedSendBuffer[sendBufferReadPos + 1];

            int dataSize;

            try
            {
                var sendArgsRemoteIpEndPoint = (IPEndPoint)sendArgs.RemoteEndPoint;

#pragma warning disable 618
                sendArgsRemoteIpEndPoint.Address.Address = (uint)
#pragma warning restore 618
                    (sharedSendBuffer[sendBufferReadPos + 2] |
                     (sharedSendBuffer[sendBufferReadPos + 3] << 8) |
                     (sharedSendBuffer[sendBufferReadPos + 4] << 16) |
                     (sharedSendBuffer[sendBufferReadPos + 5] << 24));

                sendArgsRemoteIpEndPoint.Port = sharedSendBuffer[sendBufferReadPos + 6] | (sharedSendBuffer[sendBufferReadPos + 7] << 8);

                dataSize = (firstHeaderByte << 8) | secondHeaderByte;
                dataSize &= HeaderDataLengthMask;

                Buffer.BlockCopy(sharedSendBuffer, sendBufferReadPos + InternalHeaderSize, sendArgs.Buffer, 0, dataSize);
                sendArgs.SetBuffer(0, dataSize);
            }
            catch (ObjectDisposedException)
            {
                SendError = SocketError.OperationAborted;

                return;
            }

            Thread.MemoryBarrier();

            sharedSendBuffer[sendBufferReadPos] = ResetFillFlag(firstHeaderByte);

            sendBufferReadPos += InternalHeaderSize + dataSize;

            try
            {
                //Loop prevent stack overflow on high CPU load
                while (!socket.SendToAsync(sendArgs))
                {
                    SendError = sendArgs.SocketError;

                    if (SendError != SocketError.Success) return;

                    if (Interlocked.Decrement(ref sendHeaderFillCount) == 0) return;

                    //Repeat sending (without stack fall)
                    goto ContinueSend;
                }
            }
            catch (ObjectDisposedException)
            {
                SendError = SocketError.ConnectionAborted;
            }
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs args)
        {
            try
            {
                if (args.SocketError != SocketError.Success)
                {
                    SendError = args.SocketError;

                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                SendError = SocketError.OperationAborted;

                return;
            }

            if (Interlocked.Decrement(ref sendHeaderFillCount) == 0) return;

            ContinueSend();
        }

        private void StartReceive()
        {
            try
            {
                //Loop prevent stack overflow on high CPU load
                while (!socket.ReceiveFromAsync(receiveArgs))
                {
                    ProcessReceive(null, receiveArgs);

                    if (ReceiveError != SocketError.Success) return;
                }
            }
            catch(ObjectDisposedException)
            {
                ReceiveError = SocketError.OperationAborted;
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs args)
        {
            IPEndPoint remoteIpEndPoint;
            byte[] receiveArgsBuffer;
            int packetSize;

            try
            {
                if (args.SocketError != SocketError.Success)
                {
                    ReceiveError = args.SocketError;

                    return;
                }

                remoteIpEndPoint = (IPEndPoint) args.RemoteEndPoint;
                receiveArgsBuffer = args.Buffer;
                packetSize = args.BytesTransferred;

                if(packetSize + InternalHeaderSize + InternalHeaderSize > sharedReceiveBuffer.Length)
                {
                    Debug.LogError("UDP incoming packet software drop! Incoming message is to large!");

                    goto ContinueReceive;
                }
            }
            catch(ObjectDisposedException)
            {
                ReceiveError = SocketError.OperationAborted;

                return;
            }

            if (packetSize > 0) //Skip empty packets
            {
                TryAddPacketData:

                var nextIndexAfterCurrentPacketData = receiveBufferWritePos + InternalHeaderSize + packetSize;

                var needResetCursor = nextIndexAfterCurrentPacketData + InternalHeaderSize >= sharedReceiveBuffer.Length;

                var dataLengthToPack = needResetCursor ? InternalHeaderSize : InternalHeaderSize + packetSize;

                var dataLengthToCheck = needResetCursor ? sharedReceiveBuffer.Length : dataLengthToPack;

                byte firstHeaderByte;

                while (receiveBufferWritePos + dataLengthToCheck > receiveBufferCheckPos && receiveBufferCheckPos < sharedReceiveBuffer.Length)
                {
                    firstHeaderByte = sharedReceiveBuffer[receiveBufferCheckPos];

                    if ((firstHeaderByte & (1 << HeaderFillFlagBitIndex)) != 0)
                    {
                        Debug.LogError("UDP incoming packet software drop! Receive buffer part is not ready!");

                        goto ContinueReceive;
                    }

                    if ((firstHeaderByte & (1 << HeaderResetCursorFlagBitIndex)) != 0)
                    {
                        receiveBufferCheckPos = sharedReceiveBuffer.Length;

                        break;
                    }

                    var oldDataSize = (firstHeaderByte << 8) | sharedReceiveBuffer[receiveBufferCheckPos + 1];
                    oldDataSize &= HeaderDataLengthMask;

                    if (oldDataSize == 0)
                    {
                        throw new Exception("Invalid receive behavior!");
                    }

                    receiveBufferCheckPos += InternalHeaderSize + oldDataSize;
                }

                if (needResetCursor)
                {
                    firstHeaderByte = (1 << HeaderFillFlagBitIndex) | (1 << HeaderResetCursorFlagBitIndex);

                    sharedReceiveBuffer[receiveBufferWritePos] = firstHeaderByte;

                    receiveBufferWritePos = 0;

                    receiveBufferCheckPos = 0;

                    Thread.MemoryBarrier();

                    Interlocked.Increment(ref receiveHeaderFillCount);

                    goto TryAddPacketData;
                }

                Buffer.BlockCopy(receiveArgsBuffer, 0, sharedReceiveBuffer, receiveBufferWritePos + InternalHeaderSize, packetSize);

                firstHeaderByte = (byte) ((1 << HeaderFillFlagBitIndex) | (packetSize >> 8));
                var secondHeaderByte = (byte) packetSize;

                sharedReceiveBuffer[receiveBufferWritePos + 1] = secondHeaderByte;

#pragma warning disable 618
                var remoteIp = (uint) remoteIpEndPoint.Address.Address;
#pragma warning restore 618
                var remotePort = (ushort) remoteIpEndPoint.Port;

                sharedReceiveBuffer[receiveBufferWritePos + 2] = (byte) remoteIp;
                sharedReceiveBuffer[receiveBufferWritePos + 3] = (byte) (remoteIp >> 8);
                sharedReceiveBuffer[receiveBufferWritePos + 4] = (byte) (remoteIp >> 16);
                sharedReceiveBuffer[receiveBufferWritePos + 5] = (byte) (remoteIp >> 24);

                sharedReceiveBuffer[receiveBufferWritePos + 6] = (byte) remotePort;
                sharedReceiveBuffer[receiveBufferWritePos + 7] = (byte) (remotePort >> 8);

                Thread.MemoryBarrier();

                sharedReceiveBuffer[receiveBufferWritePos] = firstHeaderByte;

                receiveBufferWritePos += dataLengthToPack;

                Thread.MemoryBarrier();

                Interlocked.Increment(ref receiveHeaderFillCount);
            }

            //If method call been async (if async, sender != null)
            ContinueReceive:
            if (sender != null) StartReceive();
        }

        public bool TryRead(byte[] buffer, int offset, out int length)
        {
            if(buffer == null) throw new ArgumentNullException(nameof(buffer));
            if(offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

            TryRead:

            if(receiveHeaderFillCount == 0)
            {
                length = 0;

                return false;
            }

            var firstHeaderByte = sharedReceiveBuffer[receiveBufferReadPos];

            if ((firstHeaderByte & (1 << HeaderFillFlagBitIndex)) == 0)
            {
                length = 0;

                return false;
            }

            if ((firstHeaderByte & (1 << HeaderResetCursorFlagBitIndex)) != 0)
            {
                sharedReceiveBuffer[receiveBufferReadPos] = ResetFillFlag(firstHeaderByte);

                receiveBufferReadPos = 0;

                if (Interlocked.Decrement(ref receiveHeaderFillCount) == 0)
                {
                    length = 0;

                    return false;
                }

                goto TryRead;
            }

            Thread.MemoryBarrier();

            var secondHeaderByte = sharedReceiveBuffer[receiveBufferReadPos + 1];

#pragma warning disable 618
            lastReadRemoteIpEndPoint.Address.Address = (uint)
#pragma warning restore 618
                 (sharedReceiveBuffer[receiveBufferReadPos + 2] |
                 (sharedReceiveBuffer[receiveBufferReadPos + 3] << 8) |
                 (sharedReceiveBuffer[receiveBufferReadPos + 4] << 16) |
                 (sharedReceiveBuffer[receiveBufferReadPos + 5] << 24));

            lastReadRemoteIpEndPoint.Port = sharedReceiveBuffer[receiveBufferReadPos + 6] | (sharedReceiveBuffer[receiveBufferReadPos + 7] << 8);

            var dataSize = (firstHeaderByte << 8) | secondHeaderByte;
            dataSize &= HeaderDataLengthMask;

            var readPos = receiveBufferReadPos + InternalHeaderSize;

            length = dataSize;

            if (length + offset > buffer.Length) throw new Exception("Invalid buffer length!");

            Buffer.BlockCopy(sharedReceiveBuffer, readPos, buffer, offset, length);

            FinishRead();

            return true;
        }

        //insert into start read
        private void FinishRead()
        {
            var firstHeaderByte = sharedReceiveBuffer[receiveBufferReadPos];

            if ((firstHeaderByte & (1 << HeaderFillFlagBitIndex)) == 0)
            {
                Debug.LogError("Could not finish read! Fill flag is zero!");

                return;
            }

            if ((firstHeaderByte & (1 << HeaderResetCursorFlagBitIndex)) != 0)
            {
                Debug.LogError("Could not finish read! Reset cursor flag is not zero!");

                return;
            }

            Thread.MemoryBarrier();

            var secondHeaderByte = sharedReceiveBuffer[receiveBufferReadPos + 1];

            var dataSize = (firstHeaderByte << 8) | secondHeaderByte;
            dataSize &= HeaderDataLengthMask;

            sharedReceiveBuffer[receiveBufferReadPos] = ResetFillFlag(firstHeaderByte);

            receiveBufferReadPos += InternalHeaderSize + dataSize;

            Interlocked.Decrement(ref receiveHeaderFillCount);
        }

        private static byte ResetFillFlag(byte source)
        {
            var value = (int) source;

            value &= ~(1 << HeaderFillFlagBitIndex);

            return (byte) value;
        }

        public void Dispose()
        {
            socket.Close();

            sendArgs.Dispose();

            receiveArgs.Dispose();
        }
    }
}