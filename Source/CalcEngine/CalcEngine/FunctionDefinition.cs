using System;
using System.Collections.Generic;
using System.Text;

namespace CalcEngine
{
    /// <summary>
    /// Function definition class (keeps function name, parameter counts, and delegate).
    /// </summary>
    public class FunctionDefinition
    {
        // ** fields
		 public string Name;
        public int ParmMin, ParmMax;
        public CalcEngineFunction Function;
		  public bool Deterministic;
          public bool Audited;

        // ** ctor
        public FunctionDefinition(string name, int parmMin, int parmMax, CalcEngineFunction function, bool deterministic, bool audited = true)
        {
			  this.Name = name;
            this.ParmMin = parmMin;
            this.ParmMax = parmMax;
            this.Function = function;
				this.Deterministic = deterministic;
                this.Audited = audited;
        }
    }
}
