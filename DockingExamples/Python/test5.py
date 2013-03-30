# python can use C# objects
# use a global object passed from C# to open a text file with file dialog



# define a convinience method using ComponentManager.MessageWriteLine
def message(*arg):
  asString = ' '.join(str(i) for i in arg)
  cm.MessageWriteLine(asString)


file = cm.OpenFileDialog()
message("OpenFileDialog return:", file);

if file:
    message("Try open file", file)
    result = cm.OpenFile(file)
    message("open file returned:", result);

