using System;
using System.Collections.Generic;
using Assets.Scripts.MainMenu.Windows;
using UnityEngine;

namespace Assets.Scripts.MainMenu
{
    public class MainMenuManager
    {
        internal readonly GameObject canvasObject;

        private readonly List<Window> windows = new List<Window>();

        private readonly LinkedList<Window> windowShowLinks = new LinkedList<Window>();

        public MainMenuManager()
        {
            canvasObject = GameObject.Find("Canvas");

            windows.Add(new MainWindow(this, canvasObject.transform.Find("MainWindow").gameObject));
            windows.Add(new JoinToGameWindow(this, canvasObject.transform.Find("JoinToGameWindow").gameObject));
            windows.Add(new JoiningToGameWindow(this, canvasObject.transform.Find("JoiningToMatchWindow").gameObject));

            ShowWindowAsTop("Main");
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && windowShowLinks.Count > 1)
            {
                HideTopWindow();
            }

            foreach (var window in windows)
            {
                window.Update();
            }
        }

        public void OnDestroy()
        {
            foreach (var window in windows)
            {
                window.OnDestroy();
            }
        }

        public void ShowWindowAsTop(string windowName)
        {
            foreach (var window in windows)
            {
                if (window.name == windowName)
                {
                    if (window.IsVisible) return;

                    if (windowShowLinks.Count > 0)
                    {
                        if (!windowShowLinks.Last.Value.IsCloseEnabled())
                        {
                            Debug.LogWarning("Could not close the window!");

                            return;
                        }

                        windowShowLinks.Last.Value.Hide();
                    }

                    var currentWindowLinkNode = windowShowLinks.First;

                    while (currentWindowLinkNode != null)
                    {
                        if(currentWindowLinkNode.Value == window)
                        {
                            windowShowLinks.Remove(currentWindowLinkNode);

                            break;
                        }

                        currentWindowLinkNode = currentWindowLinkNode.Next;
                    }

                    windowShowLinks.AddLast(window);

                    window.Show();

                    return;
                }
            }

            throw new Exception($"Could not find window by name: {windowName}!");
        }

        public Window GetWindowByName(string windowName)
        {
            foreach (var window in windows)
            {
                if (window.name == windowName) return window;
            }

            return null;
        }

        public void HideTopWindow()
        {
            if (windowShowLinks.Count == 0) return;

            if (!windowShowLinks.Last.Value.IsCloseEnabled()) return;

            windowShowLinks.Last.Value.Hide();

            windowShowLinks.RemoveLast();

            if(windowShowLinks.Count > 0) windowShowLinks.Last.Value.Show();
        }
    }
}