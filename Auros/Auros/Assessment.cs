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
        public List<Item> AssociatedItemList;

        public string VideoPath;
        public string RawDataPath;        
        public int storedRawDataNum;

        public bool isActive;

        public Assessment()
        {

            isActive = false;
            AssessmentName = string.Empty;
            AssociatedItemList = new List<Item>();
        }
    }
}
