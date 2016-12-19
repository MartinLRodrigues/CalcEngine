using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalcEngine
{

	public class Observer
	{
		public event EventHandler<EventArgs> OnPreDependencyCheck;
		public event EventHandler<EventArgs> OnPostDependencyCheck;
		public event EventHandler<EvaluateEventArgs> OnPreEvaluate;
		public event EventHandler<EvaluateEventArgs> OnPostEvaluate;
        public event EventHandler<EventArgs> OnBeginSuppress;
        public event EventHandler<EventArgs> OnEndSuppress;
		
		public void DoOnPreEvaluate(EvaluateEventArgs ea)
		{
			if (OnPreEvaluate != null)
			{
				OnPreEvaluate(this, ea);
			}
		}

		public void DoOnPostEvaluate(EvaluateEventArgs ea)
		{
			if (OnPostEvaluate != null)
			{
				OnPostEvaluate(this, ea);
			}
		}

		public void DoOnPreDependencyCheck(EventArgs ea)
		{
			if (OnPreDependencyCheck != null)
			{
				OnPreDependencyCheck(this, ea);
			}
		}

		public void DoOnPostDependencyCheck(EventArgs ea)
		{
			if (OnPostDependencyCheck != null)
			{
				OnPostDependencyCheck(this, ea);
			}
		}
        
        public void DoOnBeginSuppress(EventArgs ea)
        {
            if (OnBeginSuppress  != null)
            {
                OnBeginSuppress(this, ea);
            }
        }
    
        public void DoOnEndSuppress(EventArgs ea)
        {
            if (OnEndSuppress != null)
            {
                OnEndSuppress(this, ea);
            }
        }
    }

	public class EvaluateEventArgs : EventArgs
	{
		public Expression Expression;
		public object Value;

		public EvaluateEventArgs(Expression expression)
			: this(expression, null)
		{
		}

		public EvaluateEventArgs(Expression expression, object value)
			
		{
			this.Expression = expression;
			this.Value = value;
		}
	}

}
