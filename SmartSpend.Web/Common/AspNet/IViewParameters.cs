using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.AspNet
{
    public interface IViewParameters
    {
        string QueryParameter { get; }
        string ViewParameter { get; }
        public string OrderParameter { get; }
    }
}
