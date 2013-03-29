# python can use C# objects
# use a global object passed from C# to open a text file

result = ComponentManager.OpenFile("Python/dummy.txt")
ComponentManager.MessageWriteLine("Python open file returned: " + str(result));


