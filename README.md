# ToListinator
[The tragedy of ToList](https://mtreit.com/programming,/.net/2024/07/30/ToList.html)

As the blog post discusses, it is very common for C# programmers to waste resources by allocating lists unnecessarily. The canonical example is using `ToList().ForEeach(...)` solely because the `List<T>` type happens to have a `ForEach` method on it.

ToListinator is a Roslyn code analyzer designed to track down and help eliminate these kinds of unnecessary ToList calls with extreme prejudice, among other things.
