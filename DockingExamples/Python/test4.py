# python can use C# objects
# use a global object passed from C# to open a text file

result = cm.OpenFile("Python/dummy.txt")
cm.MessageWriteLine("Python open file returned: " + str(result));


