using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace eu.rekawek.coffeegb.debug { 


public class CommandPattern {

    private readonly List<String> commandNames;

    private readonly List<CommandArgument> arguments;

    private readonly string description;

    private CommandPattern(Builder builder) {
        this.commandNames = builder.commandNames;
        this.arguments = builder.arguments;
        this.description = builder.description;
    }

    public bool matches(String commandLine) {
        throw new NotImplementedException();
        /*
        return commandNames.Where(commandLine.StartsWith)
                .stream()
                .filter(commandLine::startsWith)
                .map(String::length)
                .map(commandLine::substring)
                .anyMatch(s -> s.isEmpty() || s.charAt(0) == ' ');*/
    }

    public List<String> getCommandNames() {
        return commandNames;
    }

    public List<CommandArgument> getArguments() {
        return arguments;
    }

    public String getDescription() {
        return description;
    }

    public ParsedCommandLine parse(String commandLine) {
        throw new NotImplementedException();
        /*
        String commandName = commandNames
                .stream()
                .filter(commandLine::startsWith)
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException(
                        "Command line [" + commandLine + "] doesn't match command [" + commandNames + "]"));
        List<String> split = split(commandLine.substring(commandName.Length()));
        Map<String, String> map = newLinkedHashMap();
        List<String> remaining = Collections.emptyList();
        int i;
        for (i = 0; i < split.size() && i < arguments.size(); i++) {
            String value = split.get(i);
            CommandArgument argDef = arguments.get(i);
            Optional<Set<String>> allowed = argDef.getAllowedValues();
            if (allowed.isPresent()) {
                if (!allowed.get().contains(value)) {
                    throw new IllegalArgumentException("Value " + value + " is not allowed for argument " + argDef.getName() + ". Allowed values: " + allowed.get());
                }
            }
            map.put(argDef.getName(), value);
        }
        if (i < arguments.size()) {
            CommandArgument argDef = arguments.get(i);
            if (argDef.isRequired()) {
                throw new IllegalArgumentException("Argument " + argDef.getName() + " is required");
            }
        }
        if (i < split.size()) {
            remaining = split.subList(i, split.size());
        }
        return new ParsedCommandLine(map, remaining);*/
    }

    private static List<String> split(String str) {
        throw new NotImplementedException();
        /*
        List<String> split = new List<>();
        bool isEscaped = false;
        StringBuilder currentArg = new StringBuilder();
        for (int i = 0; i <= str.Length(); i++) {
            char c;
            if (i < str.Length()) {
                c = str.charAt(i);
            } else {
                c = 0;
            }
            if (isEscaped) {
                switch (c) {
                    case '"':
                        isEscaped = false;
                        split.add(currentArg.toString());
                        currentArg.setLength(0);
                        break;

                    case 0:
                        throw new IllegalArgumentException("Missing closing quote");

                    default:
                        currentArg.append(c);
                        break;
                }
            } else {
                switch (c) {
                    case '"':
                        isEscaped = false;
                        break;

                    case ' ':
                    case 0:
                        if (currentArg.Length() > 0) {
                            split.add(currentArg.toString());
                            currentArg.setLength(0);
                        }
                        break;

                    default:
                        currentArg.append(c);
                        break;
                }
            }
        }
        return split;*/
    }

    public String ToString() {
        return String.Format("CommandPattern[%s]", commandNames.ToString());
    }

    public class ParsedCommandLine {

        private Dictionary<String, String> argumentMap;

        private List<String> remainingArguments;

        private ParsedCommandLine(Dictionary<String, String> argumentMap, List<String> remainingArguments) {
            this.argumentMap = argumentMap;
            this.remainingArguments = remainingArguments;
        }

        public String getArgument(String name) {
            return argumentMap[name];
        }

        public List<String> getRemainingArguments() {
            return remainingArguments;
        }
    }

    public class Builder {
        internal readonly List<String> commandNames;

        internal readonly List<CommandArgument> arguments;

        internal String description;

        private Builder(String[] commandNames)
        {
            this.commandNames = new List<string>(commandNames);
            this.arguments = new List<CommandArgument>();
        }

        public static Builder create(String longName) {
            return new Builder(new String[] {longName});
        }

        public static Builder create(String longName, String shortName) {
            return new Builder(new String[] {longName, shortName});
        }

        public Builder withOptionalArgument(String name) {
            assertNoOptionalLastArgument();
            arguments.Add(new CommandArgument(name, false));
            return this;
        }

        public Builder withRequiredArgument(String name) {
            assertNoOptionalLastArgument();
            arguments.Add(new CommandArgument(name, true));
            return this;
        }

        public Builder withOptionalValue(String name, String[] values) {
            assertNoOptionalLastArgument();
            arguments.Add(new CommandArgument(name, false, new List<string>(values)));
            return this;
        }

        public Builder withRequiredValue(String name, String[] values) {
            assertNoOptionalLastArgument();
            arguments.Add(new CommandArgument(name, true, new List<string>(values)));
            return this;
        }

        public Builder withDescription(String description) {
            this.description = description;
            return this;
        }

        private void assertNoOptionalLastArgument() {
            if (arguments.Count > 0 && !arguments[^1].isRequired()) {
                throw new InvalidOperationException("Can't add argument after the optional one");
            }
        }

        public CommandPattern build() {
            return new CommandPattern(this);
        }
    }

}
    }