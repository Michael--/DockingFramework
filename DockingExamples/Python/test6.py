
# define a convinience method using ComponentManager.MessageWriteLine
def message(*arg):
  asString = ' '.join(str(i) for i in arg)
  cm.MessageWriteLine(asString)


# Return a list containing the Fibonacci series up to n
def Fibonacci(n):
     result = []
     a, b = 0, 1
     while a < n:
         result.append(a)    # see below
         a, b = b, a+b
     return result

fib = Fibonacci(1000)
message("Fibonacci(1000):", fib)
