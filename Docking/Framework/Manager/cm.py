def app():
	'''
	get the root instance name
	'''
	return [INSTANCE];

def message(*arg):
    '''
    This is a convenience method using ComponentManager.MessageWriteLine.
    It can be used instead of print. In that case, output is done via IMessage.
    '''
    asString = ''.join(str(i) for i in arg)
    app().MessageWriteLine(asString)
    
def concat(*arg):
    '''
	concatenates the given arguments to a single string
    '''
    return ''.join(str(i) for i in arg)
