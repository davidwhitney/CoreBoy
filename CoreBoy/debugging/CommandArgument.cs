using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreBoy.debugging
{
	public class CommandArgument
    {

        private readonly string _name;

        private readonly bool _required;

        private readonly ICollection<string> _allowedValues;

        public CommandArgument(string name, bool required)
        {
            _name = name;
            _required = required;
            _allowedValues = new List<string>();
        }

        public CommandArgument(string name, bool required, ICollection<string> allowedValues)
        {
            _name = name;
            _required = required;
            _allowedValues = allowedValues ?? new List<string>();
        }

        public string GetName()
        {
            return _name;
        }

        public bool IsRequired()
        {
            return _required;
        }

        public ICollection<string> GetAllowedValues()
        {
            return _allowedValues;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!_required)
            {
                builder.Append('[');
            }

            if (_allowedValues != null)
            {
                builder.Append('{');
                builder.Append(string.Join(",", _allowedValues.ToArray()));
                builder.Append('}');
            }
            else
            {
                builder.Append(_name.ToUpper());
            }

            if (!_required)
            {
                builder.Append(']');
            }

            return builder.ToString();
        }
    }
}