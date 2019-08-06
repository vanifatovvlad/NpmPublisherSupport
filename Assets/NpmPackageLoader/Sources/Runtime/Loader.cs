using System;
using UnityEngine;

namespace NpmPackageLoader
{
    public abstract class Loader : ScriptableObject
    {
#if UNITY_EDITOR
        public virtual void Export(TextAsset packageJsonAsset, Action success, Action fail) => fail();

        public virtual void Import(TextAsset packageJsonAsset, Action success, Action fail) => fail();

        protected void ExecuteAction(Action action, Action fail)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                fail();
            }
        }
#endif
    }
}