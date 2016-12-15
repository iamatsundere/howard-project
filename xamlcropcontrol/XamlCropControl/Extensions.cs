using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XamlCropControl
{
    static class Extensions
    {
        internal static bool IsNaN(this double d)
        {
            return Double.IsNaN(d);
        }
    }
}
