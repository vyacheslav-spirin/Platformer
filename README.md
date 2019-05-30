Тестовая версия платформера с мультиплеером.

*Все интерактивные элементы в игре представлен в виде Actor.*

*Экземпляры Actor упаковываются и отсылаются клиентам (по-умолчанию 10 раз в секунду).*

*Перед отправкой сервер разделяет игровое состояние на фрагменты по 1400 байт (MTU) и отправляет их клиентам.*

*При получении всех фрагментов игрового состояния, клиент начинает читать данные и обновляет состояние Actors.*

*Для обеспечения плавности движения удаленных actors используется простая линейная интерполяция между предпоследним и последним состоянием.*

*Т.к. ссылки на все actors хранятся в обычном массиве, то для быстрого нахождения любого Actor используется структура ActorPointer, которая формируется из уникального ID,
выдаваемого новому actor и allocation id, обозначающего расположение в массиве. Комбинация unique id + allocation id дает однозначно определить,
находится ли искомый объект в массиве или же он был удален и, например, замещен новым.*

*Мультиплеерная часть игры является расширением основной части (одиночной), что позволяет в общих случаях писать логику, не отвлекаясь на сетевую часть.*

*Для передачи данных используется протокол на основе UDP. Класс UdpNetwork инкапсулирует всю логику работы с асинхронными сокетами (SocketAsyncEventArgs) 
и предоставляет удобные методы для отправки и получения данных, добавляя их во внутреннюю очередь (ring buffer) и отбрасывающий данные при переполнении буфера,
что позволяет полностью избежать аллокаций.*



