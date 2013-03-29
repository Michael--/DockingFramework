# python can use C# objects
# use a global object passed from C# to print into the message box

#direct using the C# object
ComponentManager.MessageWriteLine("Hello from Python")

# define a convinience method using ComponentManager.MessageWriteLine
def Message(*arg):
  asString = '  '.join(str(i) for i in arg)
  ComponentManager.MessageWriteLine(asString)

#using convinience, useful due to parameter types
Message("Hello number:", 4711)

#message can output also variabe lists
Message("Show a range", range(3, 11))


