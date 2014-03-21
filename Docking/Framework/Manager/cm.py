def app():
	'''
	Returns the main application object instance.
	'''
	return [INSTANCE];

def concat(*arg):
    '''
	Concatenates the given arguments into a single string.
    '''
    return ''.join(str(i) for i in arg)
	
def message(*arg):
    '''
    This function behaves the same way as print(),
	but additionally outputs its parameter(s) via IMessage to the message log.
    '''
    msg = concat(arg)
    app().MessageWriteLine(msg)
    print(msg)