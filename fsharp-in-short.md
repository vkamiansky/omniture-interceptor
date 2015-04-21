F# is a *.NET* language. It produces libraries usable from *C#* and vice versa.

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

2) Function types and type transitions in place of function calls. Pipeline and composition bring convenience.

```f#
let div b a =      // This is a function. Its type is infered from its use as float -> float -> float
   a/b 
let c = div 4. 3.  
```
Types are infered at compile time. You cannot write `let d = div 3 4` after the code above because the type of `div` has already been inferred as `float -> float -> float` through the previous statement and now this function `div` is bound to cannot provide a transition from `int`. `float -> float -> float` certainly means 'from float to float to float'
```f#
let c = div 4. 3.  // Two transitions at a time - right to the float result
let d = div 4.     // Transition to float -> float
let e = d 3.       // Transition to float 0.75
```
There's a reason why the parameters have been placed in the reverse order. Multiple subsequent transitions are usually written using the pipeline operator `|>` in the following manner:
```f#
let f = 3.            // Here's 3.0
        |> div 4.     // Devide by 4.0
        |> div 4.     // Again devide by 4.0 and bind f to the result
```
So `3. |> div 4.` is the same as `div 4. 3.`. But you can make a function as a composition of functions. For instance, `div 4.` is a function that has type `float -> float` and devides a `float` value by 4. We can compose two of such functions to devide by 8 by simply writing `div 4. >> div 4.`.
```f#
let div8 = div 4. >> div 4. // A function composed of two

let s = 3.0 |> div8
```
But what is even cooler is that operators can also be used as functions. However, `(/)` cannot be used in place of `div` for it has its parameters in a natural order. 
```f#
let div4plus4 = div 4. >> (+) 4.  // first devide, then add

let s = 3.0 |> div4plus4
```
Usually, pipelines are not used for such trivial arithmetic operations but all the list processing functions are designed specifically for pipline use.
```f#
let lst = [1;2;3]
          |> List.map (fun i -> i + 1) // Add one to each item
```
Or simpler... remember, the operators
```f#
let lst = [1;2;3]
          |> List.map ((+) 1)
```
