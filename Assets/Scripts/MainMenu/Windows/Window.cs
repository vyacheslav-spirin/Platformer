using UnityEngine;

namespace Assets.Scripts.MainMenu.Windows
{
    public abstract class Window
    {
        protected readonly MainMenuManager mainMenuManager;

        public readonly string name;

        public bool IsVisible => rootObject.activeSelf;

        protected readonly GameObject rootObject;

        protected Window(MainMenuManager mainMenuManager, string name, GameObject rootObject)
        {
            this.mainMenuManager = mainMenuManager;

            this.name = name;

            this.rootObject = rootObject;

            rootObject.SetActive(false);
        }

        public void Show()
        {
            rootObject.SetActive(true);
        }

        public void Hide()
        {
            rootObject.SetActive(false);
        }

        public virtual bool IsCloseEnabled()
        {
            return true;
        }

        public virtual void Update()
        {
        }

        public virtual void OnDestroy()
        {
        }
    }
}