using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mixin
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class MixinAttribute : Attribute
    {

#pragma warning disable IDE0060 // Remove unused parameter: It is used only at comple time for the analizer.
        public MixinAttribute(Type toImplement)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }


    }
}
