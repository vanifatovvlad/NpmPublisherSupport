using System;
using UnityEngine;

namespace NpmPackageLoader
{
    public abstract class Loader : ScriptableObject
    {
        public virtual void Export(TextAsset packageJsonAsset, Action success, Action fail) => fail();

        public virtual void Import(TextAsset packageJsonAsset, Action success, Action fail) => fail();

        protected void Try(Action onFail, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                onFail();
            }
        }
    }
}