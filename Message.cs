using System;

namespace Raavel
{
    class Message : Exception
    {
        public Message()
            : base()
        { }

        public Message(string message)
            : base(message)
        { }
    }
}
