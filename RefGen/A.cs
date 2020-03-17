using System;

namespace RefGen
{
    public class A 
    { 
        static A()
        {
            _threadStaticValue = true;
        }

        public A()
        {
            _good = false;
        }

        public void Test() 
        {
            ProtectedTest();
        } 

        protected void ProtectedTest()
        {

        }

        public bool Good
        {
            get
            {
                return _good;
            }

            private set
            {
                _good = value;
            }
        }

        internal static bool ThreadStaticValue
        {
            get
            {
                return _threadStaticValue;
            }
        }

        private bool _good;

        [ThreadStatic]
        private static bool _threadStaticValue;
    }
}