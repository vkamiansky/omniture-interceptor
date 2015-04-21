F# is a *.NET* language. It produces libraries usable in *C#* and vice versa.

F# favors *functional programming*. F# has a full set of .NET *OOP* features. 
For more F# knowledge go to http://fsharpforfunandprofit.com/

Here is some information to help you use F# 

1) Every statement has a type. Statements of the same level start with the same indent. Binding is not assignment.

```f#
4  // This statement is int
```
Single line comments same as C#.
```f#
let a = 4  // This statement is also int
```
Binding. The statement above means 'next time we ask for `a` we'll get an **immutable** value `4` this name is bound to'. 
We can rebind `a` as many times as we want.
```f#
let a = 4      // Bind name a to 4
let a = a + 1  // Get what name a was bound to, add 1, bind name a to the result
```
The value by the name `a` is immutable. If we want to change it (scenarios of interop with legacy code e.g. C#) we can do it.
```f#
let mutable a = 4      // Bind name a to mutable cell, initialize it with 4
a <- a + 1            // Get the value of what name a was bound to, add 1, send the result to cell a is bound to
```
Assignment. For multithreaded data exchange using legacy approach from C# we use *reference cells*
```f#
let a 
    = ref 4        // Bind name a to reference cell, initialize it with 4. This statement has type 'int ref'
a := !a + 1        // Get the value of the reference cell a was bound to, add 1, send the result to cell a is bound to
```
Note: the second line of the code above has indent. That means it is a continuation of the statement above, not a statement on the same level.
