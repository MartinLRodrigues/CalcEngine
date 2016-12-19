using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CalcEngine
{
	public class Audit: IDisposable
	{
		private Observer _observer = null;
		private StringBuilder _stringBuilder = new StringBuilder();
        private int _maxLevel = 0;
        private int _suppressLevel = -1;
        private HashSet<string> _auditHashes = new HashSet<string>();
		public Audit(Observer observer, bool auditVariables = false, int maxLevel = 0)
		{
			this.Observer = observer;
            this.AuditVariables = auditVariables;
            this._maxLevel = maxLevel;
		}

        public bool AuditVariables;

		protected Observer Observer
		{
			get
			{
				return _observer;
			}
			set
			{
				UnregisterEvents();
				_observer = value;
				RegisterEvents();
			}
		}

		public string AuditText
		{
			get
			{
				return _stringBuilder.ToString();
			}
		}

		public void ClearAudit()
		{
			_stringBuilder = new StringBuilder();
		}


		void _observer_OnPostEvaluate(object sender, EvaluateEventArgs e)
		{
			if (_maxLevel >=0 && _DependencyDepth > _maxLevel)
				return;

            if (_suppressLevel >= 0 && _DependencyDepth >= _suppressLevel)
                return;

			if (e.Expression._token.Type == TKTYPE.LITERAL)
				return;

            if (e.Expression._token.ID == TKID.SUPPRESS)
                return;

            if (e.Expression is VariableExpression)
            {
                if (!this.AuditVariables)
                    return;
                var v = e.Expression as VariableExpression;
                if ((v.Variable != null) && !v.Variable.CreateAudit)
                    return;
            }

            if (e.Expression is FunctionExpression)
            {
                FunctionExpression func = e.Expression as FunctionExpression;
                if (!func._fn.Audited)
                    return;
            }

            StringBuilder auditBuilder = new StringBuilder();
			auditBuilder.Append(e.Expression.ToString());
			auditBuilder.Append(" = ");

			if (e.Value != null)
			{
				if (e.Value is IEnumerable)
				{
					Expression.BuildParameterList(auditBuilder, e.Value as IEnumerable, sb => sb.AppendLine(","));
				}
				else
					auditBuilder.Append(e.Value.ToString());
			}
			auditBuilder.AppendLine();
            string auditText = auditBuilder.ToString();
            if (_auditHashes.Add(auditText))
            {
                _stringBuilder.Append(auditText);
            }
		}

		void _observer_OnPreEvaluate(object sender, EvaluateEventArgs e)
		{
			//throw new NotImplementedException();
		}

		private int _DependencyDepth = 0;

		void _observer_OnPostDependencyCheck(object sender, EventArgs e)
		{
			_DependencyDepth--;
		}

		void _observer_OnPreDependencyCheck(object sender, EventArgs e)
		{
			_DependencyDepth++;
		}

        private void _observer_OnEndSuppress(object sender, EventArgs e)
        {
            if (_suppressLevel == _DependencyDepth)
                _suppressLevel = -1;
            _DependencyDepth--;
        }

        void _observer_OnBeginSuppress(object sender, EventArgs e)
        {
            _DependencyDepth++;
            if (_suppressLevel == -1)
                _suppressLevel = _DependencyDepth;
        }

		private void UnregisterEvents()
		{
			if (_observer != null)
			{
				_observer.OnPostEvaluate -= new EventHandler<EvaluateEventArgs>(_observer_OnPostEvaluate);
				_observer.OnPreEvaluate -= new EventHandler<EvaluateEventArgs>(_observer_OnPreEvaluate);
				_observer.OnPreDependencyCheck -= new EventHandler<EventArgs>(_observer_OnPreDependencyCheck);
				_observer.OnPostDependencyCheck -= new EventHandler<EventArgs>(_observer_OnPostDependencyCheck);
                _observer.OnBeginSuppress -= new EventHandler<EventArgs>(_observer_OnBeginSuppress);
                _observer.OnEndSuppress -= new EventHandler<EventArgs>(_observer_OnEndSuppress);
			}
		}

		
		private void RegisterEvents()
		{
			if (_observer != null)
			{
				_observer.OnPostEvaluate += new EventHandler<EvaluateEventArgs>(_observer_OnPostEvaluate);
				_observer.OnPreEvaluate += new EventHandler<EvaluateEventArgs>(_observer_OnPreEvaluate);
				_observer.OnPreDependencyCheck += new EventHandler<EventArgs>(_observer_OnPreDependencyCheck);
				_observer.OnPostDependencyCheck += new EventHandler<EventArgs>(_observer_OnPostDependencyCheck);
                _observer.OnBeginSuppress += new EventHandler<EventArgs>(_observer_OnBeginSuppress);
                _observer.OnEndSuppress += new EventHandler<EventArgs>(_observer_OnEndSuppress);
			}
		}

       

		public void Dispose()
		{
			UnregisterEvents();
		}
	}
}
