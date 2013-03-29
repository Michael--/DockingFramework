# python can use C# objects
# use a global object passed from C# to open a text file with file dialog



# define a convinience method using ComponentManager.MessageWriteLine
def Message(*arg):
  asString = ' '.join(str(i) for i in arg)
  ComponentManager.MessageWriteLine(asString)


file = ComponentManager.OpenFileDialog()
Message("OpenFileDialog return:", file);

if file:
    Message("Try open file", file)
    result = ComponentManager.OpenFile(file)
    Message("open file returned:", result);

