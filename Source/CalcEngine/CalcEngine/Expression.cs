using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace CalcEngine
{
    /// <summary>
	/// Base class that represents parsed expressions.
	/// </summary>
    /// <remarks>
    /// For example:
    /// <code>
    /// Expression expr = scriptEngine.Parse(strExpression);
    /// object val = expr.Evaluate();
    /// </code>
    /// </remarks>
	public class Expression : IComparable<Expression>
	{
        //---------------------------------------------------------------------------
        #region ** fields

        internal Token _token;
        static CultureInfo _ci = CultureInfo.InvariantCulture;
		  internal Observer _observer;
        #endregion

        //---------------------------------------------------------------------------
        #region ** ctors

		  public Expression(Observer observer)
		{
            _token = new Token(null, TKID.ATOM, TKTYPE.IDENTIFIER);
				_observer = observer;
		}
        public Expression(Observer observer, object value):this(observer)
        {
            _token = new Token(value, TKID.ATOM, TKTYPE.LITERAL);
        }
		  internal Expression(Observer observer, Token tk)
			  : this(observer)
        {
            _token = tk;
        }

        #endregion

		  public Observer Observer
		  {
			  get
			  {
				  return _observer;
			  }
		  }
        //---------------------------------------------------------------------------
        #region ** object model

		public object Evaluate()
		{
			object result;
            PreEvaluate();
			  
			result = this.EvaluateExpression();

            PostEvaluate(result);
			  
			return result;
		}

        public void PostEvaluate(object result)
        {
            if (_observer != null)
                _observer.DoOnPostEvaluate(new EvaluateEventArgs(this, result));
        }

        public void PreEvaluate()
        {
            if (_observer != null)
                _observer.DoOnPreEvaluate(new EvaluateEventArgs(this));
        }

        public void PostDependencyCheck()
        {
            if (_observer != null)
                _observer.DoOnPostDependencyCheck(new EventArgs());
        }

        public void PreDependencyCheck()
        {
            if (_observer != null)
                _observer.DoOnPreDependencyCheck(new EventArgs());
        }

        public void BeginSuppress()
        {
            if (_observer != null)
                _observer.DoOnBeginSuppress(new EventArgs());
        }
        
        public void EndSuppress()
        {
            if (_observer != null)
                _observer.DoOnEndSuppress(new EventArgs());
        }

		protected virtual object EvaluateExpression()
		{
            if (_token.Type != TKTYPE.LITERAL)
            {
                throw new ArgumentException("Bad expression.");
            }
			  
			  object result = _token.Value;
			  //audit.AppendFormat("{0} = {1}",this.ToString(), result);
			  
			  return result;
		}
        public virtual Expression Optimize()
        {
            return this;
        }

        #endregion

        //---------------------------------------------------------------------------
        #region ** implicit converters

        public static implicit operator string(Expression x)
        {
            var v = x.Evaluate();
            return v == null ? string.Empty : v.ToString();
        }
        public static implicit operator double(Expression x)
        {
            // evaluate
            var v = x.Evaluate();

            // handle doubles
            if (v is double)
            {
                return (double)v;
            }

            // handle booleans
            if (v is bool)
            {
                return (bool)v ? 1 : 0;
            }

            // handle dates
            if (v is DateTime)
            {
                return ((DateTime)v).ToOADate();
            }

            // handle nulls
            if (v == null)
            {
                return 0;
            }

            // handle everything else
            return (double)Convert.ChangeType(v, typeof(double), _ci);
        }
        public static implicit operator bool(Expression x)
        {
            // evaluate
            var v = x.Evaluate();

            // handle booleans
            if (v is bool)
            {
                return (bool)v;
            }

            // handle nulls
            if (v == null)
            {
                return false;
            }

            // handle doubles
            if (v is double)
            {
                return (double)v == 0 ? false : true;
            }

            // handle everything else
            return (double)x == 0 ? false : true;
        }
        public static implicit operator DateTime(Expression x)
        {
            // evaluate
            var v = x.Evaluate();

            // handle dates
            if (v is DateTime)
            {
                return (DateTime)v;
            }

            // handle doubles
            if (v is double)
            {
                return DateTime.FromOADate((double)x);
            }

            // handle everything else
            return (DateTime)Convert.ChangeType(v, typeof(DateTime), _ci);
        }

		 
        #endregion

        //---------------------------------------------------------------------------
        #region ** IComparable<Expression>

        public int CompareTo(Expression other)
        {
            // get both values
            var c1 = this.Evaluate() as IComparable;
            var c2 = other.Evaluate() as IComparable;
            
            // handle nulls
            if (c1 == null && c2 == null)
            {
                return 0;
            }
            if (c2 == null)
            {
                return -1;
            }
            if (c1 == null)
            {
                return +1;
            }

            // make sure types are the same
            if (c1.GetType() != c2.GetType())
            {
                if (c1 is IConvertible)
                    c2 = Convert.ChangeType(c2, (c1 as IConvertible).GetTypeCode()) as IComparable;
                else
                    c2 = Convert.ChangeType(c2, c1.GetType()) as IComparable;
            }

            // compare
            return c1.CompareTo(c2);
        }

        #endregion

		  public override string ToString()
		  {
			  return (_token.Value == null)? string.Empty : _token.Value.ToString();
		  }

          public static void BuildList(StringBuilder sb, IEnumerable objects, Action<StringBuilder> appendMethod)
		  {
              BuildList(sb, objects, (sb2, o) => BuildParameterList(sb2, o, appendMethod), appendMethod);
		  }

		  public static void BuildList(StringBuilder sb, IEnumerable objects, Action<StringBuilder, object> method, Action<StringBuilder> appendMethod)
		  {
			  bool firstTime = true;
			  if (objects != null)
			  {
				  foreach (object value in (objects))
				  {
                      if (firstTime)
                          firstTime = false;
                      else
                          appendMethod(sb);

					  method(sb, value);
				  }
			  }
		  }
          public static void BuildParameterList(StringBuilder sb, object p, Action<StringBuilder> appendMethod)
		  {
			  if (p is IEnumerable)
			  {
                  sb.AppendLine("{");
				  BuildList(sb, p as IEnumerable, appendMethod);
				  sb.Append(" }");
			  }
			  else
				  sb.Append(p.ToString());
		  }
	}
    /// <summary>
    /// Unary expression, e.g. +123
    /// </summary>
	class UnaryExpression : Expression
	{
        // ** fields
		Expression	_expr;

        // ** ctor
		public UnaryExpression(Observer observer, Token tk, Expression expr) : base(observer, tk)
		{
			_expr = expr;
		}

        // ** object model
		override protected object EvaluateExpression()
		{
            switch (_token.ID)
			{
				case TKID.ADD:
                    return +(double)_expr;
				case TKID.SUB:
                    return -(double)_expr;
                case TKID.SUPPRESS:
                    this.BeginSuppress();
                    object result = _expr.Evaluate();
                    this.EndSuppress();
                    return result;
			}
			throw new ArgumentException("Bad expression.");
		}
        public override Expression Optimize()
        {
            _expr = _expr.Optimize();
            return _expr._token.Type == TKTYPE.LITERAL
                ? new Expression(_observer, this.EvaluateExpression())
                : this;
        }

		  public override string ToString()
		  {
			  return base.ToString() + " " + _expr.ToString();
		  }
	}
    /// <summary>
    /// Binary expression, e.g. 1+2
    /// </summary>
	class BinaryExpression : Expression
	{
        // ** fields
		Expression	_lft;
		Expression	_rgt;

        // ** ctor
		public BinaryExpression(Observer observer, Token tk, Expression exprLeft, Expression exprRight) : base(observer, tk)
		{
			_lft  = exprLeft;
			_rgt = exprRight;
		}

        // ** object model
		override protected object EvaluateExpression()
		{
			// handle comparisons
            if (_token.Type == TKTYPE.COMPARE)
            {
                var cmp = _lft.CompareTo(_rgt);
                switch (_token.ID)
                {
						 case TKID.GT: return cmp > 0;
						 case TKID.LT: return cmp < 0;
						 case TKID.GE: return cmp >= 0;
						 case TKID.LE: return cmp <= 0;
						 case TKID.EQ: return cmp == 0;
						 case TKID.NE: return cmp != 0;
                }
            }

            // handle everything else
            switch (_token.ID)
			{
				case TKID.ADD: 
                    return (double)_lft + (double)_rgt;
				case TKID.SUB:
						  return (double)_lft - (double)_rgt;
				case TKID.MUL:
						  return (double)_lft * (double)_rgt;
				case TKID.DIV:
						  return (double)_lft / (double)_rgt;
				case TKID.DIVINT:
						  return  (double)(int)((double)_lft / (double)_rgt);
				case TKID.MOD:
						  return (double)(int)((double)_lft % (double)_rgt);
				case TKID.POWER:
                    var a = (double)_lft;
                    var b = (double)_rgt;
                    if (b == 0.0) return 1.0;
                    if (b == 0.5) return Math.Sqrt(a);
                    if (b == 1.0) return a;
                    if (b == 2.0) return a * a;
                    if (b == 3.0) return a * a * a;
                    if (b == 4.0) return a * a * a * a;
                    return Math.Pow((double)_lft, (double)_rgt);
			}
			throw new ArgumentException("Bad expression.");
		}
        public override Expression Optimize()
        {
            _lft = _lft.Optimize();
            _rgt = _rgt.Optimize();
            return _lft._token.Type == TKTYPE.LITERAL && _rgt._token.Type == TKTYPE.LITERAL
                ? new Expression(_observer, this.EvaluateExpression())
                : this;
        }

		  public override string ToString()
		  {
			  return string.Format("({0}) {1} ({2})", _lft.ToString(), base.ToString(), _rgt.ToString());
		  }
    }
    /// <summary>
    /// Function call expression, e.g. sin(0.5)
    /// </summary>
	public class FunctionExpression : Expression
    {
        // ** fields
        internal FunctionDefinition _fn;
        List<Expression> _parms;

        // ** ctor
        internal FunctionExpression(Observer observer): base(observer)
        {
        }
        public FunctionExpression(Observer observer, FunctionDefinition function, List<Expression> parms): base(observer)
        {
            _fn = function;
            _parms = parms;
        }

        // ** object model
        override protected object EvaluateExpression()
        {
            return _fn.Function(_parms);
        }

        public override Expression Optimize()
        {
            bool allLits = true;
            
            if (_parms != null)
            {
                for (int i = 0; i < _parms.Count; i++)
                {
                    var p = _parms[i].Optimize();
                    _parms[i] = p;
                    if (p._token.Type != TKTYPE.LITERAL)
                    {
                        allLits = false;
                    }
                }
            }
            // only deterministic functions with literal parameters can be optimized
            return allLits && _fn.Deterministic
                ? new Expression(_observer, this.EvaluateExpression())
                : this;
        }
		  public override string ToString()
		  {
			  StringBuilder sb = new StringBuilder();
			  sb.Append(_fn.Name);
			  sb.Append("(");
			  Expression.BuildList(sb, _parms, s => s.Append(", "));
			  sb.Append(")");
			  return sb.ToString();
		  }
    }

    /// <summary>
    /// Simple variable reference.
    /// </summary>
    class VariableExpression : Expression
    {
        Dictionary<string, Variable> _dct;
        string _name;

        public Variable Variable
        {
            get
            {
                return _dct[_name];
            }
        }

        public VariableExpression(Observer observer, Dictionary<string, Variable> dct, string name): base(observer)
        {
            _dct = dct;
            _name = name;
        }
        protected override object EvaluateExpression()
        {
            Variable v = this.Variable;
            if (v != null)
            {
                return v.Value;
            }
            return null;
		  }

		  public override string ToString()
		  {
              Variable v = this.Variable;
              if (v != null)
                  return v.ToString();

			  return _name;
		  }
    }
    /// <summary>
    /// Expression based on an object's properties.
    /// </summary>
    class BindingExpression : Expression
    {
        CalcEngine _ce;
        CultureInfo _ci;
        List<BindingInfo> _bindingPath;

        // ** ctor
        internal BindingExpression(Observer observer, CalcEngine engine, List<BindingInfo> bindingPath, CultureInfo ci): base(observer)
        {
            _ce = engine;
            _bindingPath = bindingPath;
            _ci = ci;
        }

        // ** object model
        override protected object EvaluateExpression()
        {
            return GetValue(_ce.DataContext);
        }

        // ** implementation
        object GetValue(object obj)
        {
            const BindingFlags bf =
                BindingFlags.IgnoreCase |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.Static;

            if (obj != null)
            {
                foreach (var bi in _bindingPath)
                {
                    // get property
                    if (bi.PropertyInfo == null)
                    {
                        bi.PropertyInfo = obj.GetType().GetProperty(bi.Name, bf);
                    }

                    // get object
                    try
                    {
                        obj = bi.PropertyInfo.GetValue(obj, null);
                    }
                    catch
                    {
                        // REVIEW: is this needed?
                        System.Diagnostics.Debug.Assert(false, "shouldn't happen!");
                        bi.PropertyInfo = obj.GetType().GetProperty(bi.Name, bf);
                        bi.PropertyInfoItem = null;
                        obj = bi.PropertyInfo.GetValue(obj, null);
                    }

                    // handle indexers (lists and dictionaries)
                    if (bi.Parms != null && bi.Parms.Count > 0)
                    {
                        // get indexer property (always called "Item")
                        if (bi.PropertyInfoItem == null)
                        {
                            bi.PropertyInfoItem = obj.GetType().GetProperty("Item", bf);
                        }

                        // get indexer parameters
                        var pip = bi.PropertyInfoItem.GetIndexParameters();
                        var list = new List<object>();
                        for (int i = 0; i < pip.Length; i++)
                        {
                            var pv = bi.Parms[i].Evaluate();
                            pv = Convert.ChangeType(pv, pip[i].ParameterType, _ci);
                            list.Add(pv);
                        }

                        // get value
                        obj = bi.PropertyInfoItem.GetValue(obj, list.ToArray());
                    }
                }
            }

            // all done
            return obj;
        }
		  public override string ToString()
		  {
			  StringBuilder sb = new StringBuilder();
			  sb.Append(_ce.DataContext.GetType().Name);
			  foreach (BindingInfo bi in _bindingPath)
			  {
				  sb.Append(".");
				  sb.Append(bi.Name);
			  }
			  return sb.ToString();
		  }
    }
    /// <summary>
    /// Helper used for building BindingExpression objects.
    /// </summary>
    class BindingInfo
    {
        public BindingInfo(string member, List<Expression> parms)
        {
            Name = member;
            Parms = parms;
        }
        public string Name { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public PropertyInfo PropertyInfoItem { get; set; }
        public List<Expression> Parms { get; set; }
    }
    /// <summary>
    /// Expression that represents an external object.
    /// </summary>
    class XObjectExpression : 
        Expression, 
        IEnumerable
    {
        object _value;

        // ** ctor
        internal XObjectExpression(Observer observer, object value): base(observer)
        {
            _value = value;
        }

        // ** object model
        protected override object EvaluateExpression()
        {
            // use IValueObject if available
            var iv = _value as IValueObject;
				if (iv != null)
            {
                return iv.GetValue();
            }

            // return raw object
            return _value;
        }
        public IEnumerator GetEnumerator()
        {
            var ie = _value as IEnumerable;
            return ie != null ? ie.GetEnumerator() : null;
        }
    }
    /// <summary>
    /// Interface supported by external objects that have to return a value
    /// other than themselves (e.g. a cell range object should return the 
    /// cell content instead of the range itself).
    /// </summary>
    public interface IValueObject
    {
        object GetValue();
    }
}
