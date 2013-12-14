using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MettleLib
{
    public interface ITagErrorInterface
    {
        void UpdateEvent(string s);
        void Initialize();
        void Reset();
    }
}
