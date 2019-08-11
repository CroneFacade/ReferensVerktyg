using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferensVerktygAlpha
{
    public class ReferenceList
    {
        public string FileName { get; set; }
        public string[] Headers { get; set; }
        public List<string[]> Rows { get; set; }
    }
}
