# python can use C# objects
# use a global object passed from C# to print into the message box

#direct using the C# object
cm.MessageWriteLine("Hello from Python")

# define a convinience method using ComponentManager.MessageWriteLine
def message(*arg):
  asString = '  '.join(str(i) for i in arg)
  cm.MessageWriteLine(asString)

#using convinience, useful due to parameter types
message("Hello number:", 4711)

#message can output also variabe lists
message("Show a range", range(3, 11))

#output can be redirected to any object which implement method write and property softspace
import sys
sys.stdout=cmd
print "Redirected", 4711, range(50, 60)



