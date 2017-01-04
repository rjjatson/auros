using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auros
{
    public class Assessment
    {
        public Definitions.AssessmentCode AssessmentCode;
        public string AssessmentName { get; set; }

        public string VideoPath;
        public string RawDataPath;        
        public int storedRawDataNum;

        public List<Item> AssociatedItemList;

        public Assessment()
        {
            AssociatedItemList = new List<Item>();
        }
    }
}
