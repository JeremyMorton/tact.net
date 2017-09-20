using System;
using Tact.Reflection;

namespace Tact
{
    public static class DelegateExtensions
    {
        public static EfficientInvoker GetInvoker(this Delegate del)
        {
            return EfficientInvoker.ForDelegate(del);
        }

        public static EfficientInvoker2 GetInvoker2(this Delegate del)
        {
            return EfficientInvoker2.ForDelegate(del);
        }
    }
}