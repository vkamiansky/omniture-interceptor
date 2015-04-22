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
The value by the name `a` is immutable. If we want to change it (scenarios of interop with legacy code e.g. C#) we can do this.
```f#
let mutable a = 4      // Bind name a to mutable cell, initialize it with 4
a <- a + 1             // Get the value of what name a was bound to, add 1, 
                       // send the result to cell a is bound to
```
Assignment. For multithreaded data exchange using legacy approach from C# we use *reference cells*
```f#
let a              // Bind name a to reference cell, initialize it with 4.
    = ref 4        // This statement has type 'int ref'  
a := !a + 1        // Get the value of the reference cell a was bound to, 
                   // add 1, send the result to cell a is bound to
```
Note: the second line of the code above is indented. That means it is a continuation of the statement above, not a statement on the same level.

2) Function types and type transitions in place of function calls. Pipeline and composition bring convenience. 

```f#
let div b a =      // This is a function. Its type is infered from its use as float -> float -> float
   a/b 
let c = div 4. 3.  
```
Types are infered at compile time. You cannot write `let d = div 3 4` after the code above because the type of the function `div` is bound to has already been inferred as `float -> float -> float` from the statement `let c = div 4. 3.` and cannot provide a transition from `int`. `float -> float -> float` certainly means 'from float to float to float'
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
So `3. |> div 4.` is the same as `div 4. 3.`. However,  you can make a function as a composition of functions. For instance, `div 4.` is a function that has type `float -> float` and devides a `float` value by 4. We can compose two of such functions to devide by 16 by simply writing `div 4. >> div 4.`.
```f#
let div16 = div 4. >> div 4. // A function composed of two

let s = 3. |> div16
```
But what is even cooler is that operators can also be used as functions.  
```f#
let div4plus4 = div 4. >> (+) 4.  // First devide, then add

let s = 3.0 |> div4plus4
```
The operator `(/)` cannot be used in place of `div` because it has its parameters in a natural order, i.e. `(/) 3. 4.` yields `0.75`. However, we can design a function that swaps arguments and still use (/) with the *pipeline operator*.
```f#
let rev f a b = f b a // Swap arguments

let div16 = rev (/) 4. >> rev (/) 4. // A function composed of two

let s = 3. |> div16 // Same result 
```
Usually, pipelines are not used for such trivial arithmetic operations but all the list processing functions are designed specifically for pipline use.
```f#
let lst = [1; 2; 3]
          |> List.map (fun i -> i + 1) // Add one to each item
```
Or simpler... remember, the operators
```f#
let lst = [1; 2; 3]
          |> List.map ((+) 1)
```
If a statement produces no value, its type is `unit`. You can turn a value of any type into `unit` by using the function `ignore`.
```f#
let proced a =        // This function has type int -> unit
    a + a |> ignore   
```


3) Tuples, lists, arrays 

Tuples have types that are product of several types. If you see something **comma** delimited it's a **tuple**.
```f#
let tpl = 3., 4. // A tuple of type  float * float
```
Join a fev values into a tuple you can use the tuple to complete as many stages of type transition as you specify. It's done like this: 
```f#
let s2 = (3., 4.) ||> (/)        // It's just another way to devide 3. by 4. 
let s3 = ((/), 4., 3.) |||> rev  // And yet another way to do this (see the rev function above)
let s2 = ((/), 4.) ||> rev       // 2 out of three transitions leave us with float -> float
let s2 = 3. |> s2                // And the same result here 
```
Too difficult? No? By now your mind muscule should be warm and ready. To the lists and arrays stuff.
```f#
let lst = [3.; 4.; 5.]       // lst is bound to a float list
let arr = [|3.; 4.; 5.|]     // arr is bound to an array (float [])
let elem = arr.[0]           // This is the way we use indices
```
Want to refresh your memory on the difference between lists and arrays. Here's a [link] (http://coders-corner.net/2014/03/09/f-array-vs-list/). Obviously, you can use list functions with lists and array functions with arrays. Here are some of them.
```f#
let lst = [3.; 4.; 5.]
          |> List.map (fun x -> x * x) // One to one function. Each elem squared
```
The result is `[9.; 16.; 25.]`.
```f#
let lst = [3.; 4.; 5.]
          |> List.collect (fun x -> [x ; -x]) // One to many. Each elem adds itself
                                              // and itself negated to result list
```
The result is `[3.; -3.; 4.; -4.; 5.; -5.]`.
```f#
[3.; 4.; 5.]
|> List.iter (printf "%f ")      // Unit. Goes through elements and returns no value
                                 // In this case prints out the elements
```
[`printf`](https://msdn.microsoft.com/en-us/library/ee370560.aspx) is an F# way to send something to console.
```f#
let sum = [3.; 4.; 5.]
          |> List.fold (+) 0.     // Many to one. Goes through elements and applies a function
                                  // that turns the accumulated value and the current value into
                                  // a new accumulated value. It returns the resulting accumulated
                                  // value. Here we specify the initial accumulated value as 0.
```
The result is `12.`.
```f#
let sum = [|3.; 4.; 5.|]
          |> Array.fold (+) 0.    // map, collect, iter and fold also exist for arrays
```

4) Variant types, pattern matching, recursive calls.

A very popular variant type is `'T option`. Here `'T` is a type parameter which means the option type can go with any other type. Here's how a definition of such type looks like
```f#
type Option<'a> =       
   | Some of 'a           // It is either Some (value of type 'a)...
   | None                 // or none
```
So, `Some 1.` has type `float option`. Let's see how it is used in various list functions.
```f#
let lst = [3.; 4.; 5.]
let found = lst
            |> List.tryFind ((=) 4.)  // Many to one. The element found. Returns Some 4.
let notFound = lst      
               |> List.tryFind ((=) 6.)  // The element not found. Returns None.  
```
The filtering function `List.choose` uses this type to determine whether to add an element to the results list
```f#
let lst = [3.; 4.; 5.]
let chosen = lst
             |> List.choose (fun x ->              // Many to one. Goes through the list
                  if x<>4. then Some x else None)  // with the specified function and adds to the result list
                                                   // the value when the function returns Some value 
```
The result is `[3.; 5.]`.
Using such types starts making sense when we later use patterns matching. 
```f#
let found = lst
            |> List.tryFind ((=) 4.) 
            
let res = 
    match found with 
    | Some f -> sprintf "Found %f" f  // If found is Some f return a formatted string with f
    | None -> "Not found"             // If found is None return "Not found"
```
The result is `Found 4.000000`. That was value matching. Ah, and as you probably guessed sprintf did string formatting for us. You can also match more complex patterns. Remember what commas mean? Then here's another example.
```f#
let found4 = lst
            |> List.tryFind ((=) 4.) 
let found5 = lst
            |> List.tryFind ((=) 5.)
            
let res = 
    match found4, found5 with 
    | None, _ | _, None -> "Not found"            // If one of the coordinates in the tuple is None
    | Some a, Some b -> sprintf "Found %f %f" a b 
```
The result is `Found 4.000000 5.000000`. And there you saw an if pattern `|` in which `_` means 'any value'.
Patterns can contain complex conditions.
```f#
let res = match "hello" with               
          | s when s.Contains "hell" -> "It's there"           
          | _ -> "It's not there"
```
The result is `It's there`.
Matching lists is just as easy.
```f#
let lst = [3.; 4.; 5.]
let chosen = lst
             |> List.choose (fun x ->              
                  if x<>4. then Some x else None)  
let res =
    match chosen with 
    | [] -> "No results"           
    | lst -> sprintf "Found %d results" (lst |> List.length)
```
The result is `Found 2 results`. Patterns are matched top to bottom, so be careful to place more specific ones before more general ones. By the way, there's a way to use pattern matching in the pipeline. Here's ho it's done.
```f#
let lst = [3.; 4.; 5.]
let res = lst
             |> List.choose (fun x ->              
                  if x<>4. then Some x else None)  
             |> function               
                | [] -> "No results"           
                | lst -> sprintf "Found %d results" (lst |> List.length)
```
The same result we obtain. 
Each list when it has items in it also has two things: a **head** which is the first element in the list and a **tail** which is the rest of the list. In patterns the *head* is separated from the *tail* through the use of the `::` operator. This operator can be used to create effective recursive algorithms. For instance, consider the function skip.
```f#
let rec skip n (lst : 'a list) =
    match lst with
    | _ :: tail -> if n <= 0 then lst else tail |> skip(n - 1)
    | _ -> []
```    
The function skips first n elements of the list and returns the rest. It has the keyword `rec` in its signature that means 'recursive'. Recursive functions are optimized to consume as little memory as usual loop-based ones. The keyword tells the compiler that it should treat the function in a special way as it uses recursion.
The head-tail operator can also be used to assemble lists element by element. They are not just for pattern use. Consider this.
```f#
let rec take n (lst : 'a list) =
    match lst with
    | head::tail -> if n > 0 then head :: (tail |> take(n - 1)) else []
    | _ -> []
``` 
The function returns first n elements of the list. 

By now you should have enough F# knowledge to read the sources of the Omniture Interceptor and to continue studying F#. 
