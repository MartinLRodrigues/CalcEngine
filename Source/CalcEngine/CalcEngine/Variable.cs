using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalcEngine
{
    public class Variable
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public bool CreateAudit { get; set; }
        public bool DisplayValue { get; set; }

        public Variable(string name, object value)
        {
            this.Name = name;
            this.Value = value;
            this.CreateAudit = false;
        }

        public override string ToString()
        {
            if (DisplayValue && this.Value != null)
                return this.Value.ToString();
            else
                return this.Name;
        }


    }
}
