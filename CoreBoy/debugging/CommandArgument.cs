using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreBoy.debugging
{
	public class CommandArgument
    {

        private readonly string name;

        private readonly bool required;

        private readonly ICollection<string> allowedValues;

        public CommandArgument(string name, bool required)
        {
            this.name = name;
            this.required = required;
            this.allowedValues = new List<string>();
        }

        public CommandArgument(string name, bool required, ICollection<string> allowedValues)
        {
            this.name = name;
            this.required = required;
            this.allowedValues = allowedValues ?? new List<string>();
        }

        public string getName()
        {
            return name;
        }

        public bool isRequired()
        {
            return required;
        }

        public ICollection<string> getAllowedValues()
        {
            return allowedValues;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!required)
            {
                builder.Append('[');
            }

            if (allowedValues != null)
            {
                builder.Append('{');
                builder.Append(string.Join(",", allowedValues.ToArray()));
                builder.Append('}');
            }
            else
            {
                builder.Append(name.ToUpper());
            }

            if (!required)
            {
                builder.Append(']');
            }

            return builder.ToString();
        }
    }
}