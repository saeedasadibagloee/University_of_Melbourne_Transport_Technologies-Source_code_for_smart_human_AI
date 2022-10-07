using System.Collections;

namespace Assets.Scripts
{
    public class ThreadedJob
    {
        private bool _mIsDone = false;
        private readonly object _mHandle = new object();
        private System.Threading.Thread _mThread = null;
        public bool IsDone
        {
            get
            {
                bool tmp;
                lock (_mHandle)
                {
                    tmp = _mIsDone;
                }
                return tmp;
            }
            set
            {
                lock (_mHandle)
                {
                    _mIsDone = value;
                }
            }
        }

        public virtual void Start()
        {
            _mThread = new System.Threading.Thread(Run);
            _mThread.Start();
        }
        public virtual void Abort()
        {
            _mThread.Abort();
        }

        protected virtual void ThreadFunction() { }

        protected virtual void OnFinished() { }

        public virtual bool Update()
        {
            if (!IsDone) return false;
            OnFinished();
            return true;
        }
        IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }
        private void Run()
        {
            ThreadFunction();
            IsDone = true;
        }
    }
}