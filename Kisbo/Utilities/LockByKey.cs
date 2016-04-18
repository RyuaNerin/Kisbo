using System;
using System.Collections.Generic;
using System.Threading;

namespace Kisbo.Utilities
{
    public sealed class LockByKey<T>
    {
        private sealed class LockKey : IDisposable
        {
            public LockKey(LockByKey<T> container, T key)
            {
                this.m_container = container;
                this.m_key = key;

                lock (this.m_container.m_dic)
                    this.m_container.m_dic.Add(key);
            }
            public void Dispose()
            {
                lock (this.m_container.m_dic)
                    this.m_container.m_dic.Remove(this.m_key);

                GC.SuppressFinalize(this);
            }

            private readonly LockByKey<T> m_container;
            private readonly T m_key;
            public T Key { get { return this.m_key; } }
        }

        private IList<T> m_dic = new List<T>();

        public void Wait(T key)
        {
            while (true)
            {
                lock (this.m_dic)
                    if (!this.m_dic.Contains(key))
                        return;
                Thread.Sleep(100);
            }
        }
        public IDisposable GetLock(T key)
        {
            return new LockKey(this, key);
        }
    }
}
