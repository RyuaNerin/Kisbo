using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Kisbo.Utilities
{
    public sealed class InstanceHelperEx : IDisposable
    {
        private const uint CustomMsg = 0x7A90;
        private static readonly IntPtr CustomParam = new IntPtr(0x7A91);

        private readonly string m_uniqueName;
        public InstanceHelperEx(string uniqueName)
        {
            this.m_uniqueName = uniqueName;

            try
            {
                bool createdNew;
                this.m_mutex = new Mutex(true, this.m_uniqueName, out createdNew);
                this.m_isInstance = createdNew || this.m_mutex.WaitOne(0);
            }
            catch
            {
                this.m_isInstance = false;
            }
        }
        public void Dispose()
        {
            this.Release();

            GC.SuppressFinalize(this);
        }

        public delegate void DataReceivedHandler(byte[] data);
        public event DataReceivedHandler DataReceived;

        private readonly bool m_isInstance;
        private Mutex m_mutex;
        private NamedPipeServerStream m_pipe;

        public bool IsInstance
        {
            get { return this.m_isInstance; }
        }

        public void Ready()
        {
            this.m_pipe = new NamedPipeServerStream(this.m_uniqueName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte);

            Task.Factory.StartNew(new Action(this.RecieveData));
        }

        public void Release()
        {
            if (this.m_mutex != null)
            {
                this.m_mutex.Dispose();
                this.m_mutex = null;
            }
            if (this.m_pipe != null)
            {
                this.m_pipe.Dispose();
                this.m_pipe = null;
            }
        }

        private void RecieveData()
        {
            var buff = new byte[4096];
            int read;

            using (var mem = new MemoryStream(10240))
            {
                while (true)
                {
                    try
                    {
                        this.m_pipe.WaitForConnection();

                        mem.SetLength(0);
                        while ((read = this.m_pipe.Read(buff, 0, 4096)) > 0)
                            mem.Write(buff, 0, read);

                        if (this.DataReceived != null)
                            this.DataReceived.Invoke(mem.ToArray());

                        this.m_pipe.Disconnect();
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }

        public void Send(byte[] data, int timeOut = 2000)
        {
            if (this.m_isInstance) return;

            var endTime = DateTime.UtcNow.AddMilliseconds(timeOut);
            do
            {
                if (SendData(data))
                    break;
                Thread.Sleep(50);
            } while (endTime < DateTime.UtcNow);
        }

        private bool SendData(byte[] data)
        {
            try
            {
                using (var pipe = new NamedPipeClientStream(".", this.m_uniqueName, PipeDirection.Out))
                {
                    pipe.Connect(50);

                    pipe.Write(data, 0, data.Length);
                    pipe.Flush();

                    pipe.WaitForPipeDrain();
                    return true;

                }
            }
            catch
            {
            }

            return false;
        }
    }
}
