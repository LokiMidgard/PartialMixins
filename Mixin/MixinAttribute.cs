using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mixin
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class MixinAttribute : Attribute
    {

        public MixinAttribute(Type toImplement)
        {
        }


    }
}
