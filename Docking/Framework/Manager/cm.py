def message(*arg):
    '''
    this is a convinience method using ComponentManager.MessageWriteLine
    can be used istead of print, output done with IMessage if the message window exist
    '''
    asString = ' '.join(str(i) for i in arg)
    cm.MessageWriteLine(asString)
    
def quit():
    '''
    exit program immediately
    '''
    cm.quit()
    

