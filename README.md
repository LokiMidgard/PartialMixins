[![NuGet](https://img.shields.io/nuget/v/PartialMixin.svg?style=flat-square)](https://www.nuget.org/packages/PartialMixin/)
[![Build status](https://ci.appveyor.com/api/projects/status/u5b72juufb2gxve3?svg=true)](https://ci.appveyor.com/project/LokiMidgard/partialmixins)
[![GitHub license](https://img.shields.io/github/license/LokiMidgard/PartialMixins.svg?style=flat-squar)](https://tldrlegal.com/license/mit-license#summary)


# PartialMixins <img src="https://raw.githubusercontent.com/LokiMidgard/PartialMixins/master/combine.png" width="35px" height="35px" />
Extends C# with Mixins. The Mixins are simulated using partial classes, instead of ilweaving like other
librarys. The MixinAttribute copys all members of the targeted Mixin to a partial class implementation
of the anotated type. This give you intellisense support on your classes. It uses the roslyn code generation framework.

## Usage

First add the NuGet [package](https://www.nuget.org/packages/PartialMixin/) to your Project.


To create a Mixin just declare a class.

```c#
    class IdMixin
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
```

To apply the mixin you need to declare the MixinAttribute on the class that should implement the desired
mixin. This class must also have the ```partial``` modifyer. Pass the Type of the Mixin in the attribute
constructor. After the next time you build your source the mixin is implemented by your class. 

```c#
    [Mixin(typeof(IdMixin))]
    partial class BusinessObject
    {
        public string Name { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var b1 = new BusinessObject() { Name = "Paul" };
            Console.WriteLine($"{b1.Id}:{b1.Name}");
        }

    }
```

Your mixins can also implement interfaces. 

```c#
    interface Id : IEquatable<Id>
    {
        Guid Id { get; }
    }

    class IdMixin : Id
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Equals(Id other) => Id.Equals(other.Id);

    }
```

If you use the Mixin Type itself, it will be substitueded with the consuming type.
```c#
    abstract class AddMixin
    {
        public abstract AddMixin Add(AddMixin other);
        public static AddMixin operator +(AddMixin a1, AddMixin a2)
        {
            return a1.Add(a2);
        }
    }
```

A Type that uses this Mixin may look like this:
```c#
namespace Sample
{
    [Mixin(typeof(AddMixin))]
    public partial struct MyNumber
    {
        public int Value { get; }
        public MyNumber(int value) => this.Value = value;
        public partial MyNumber Add(MyNumber other) => new MyNumber(this.Value + other.Value);
    }
}

// Generated Code...

namespace Sample
{
    public partial struct MyNumber
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Mixin Task", "1.0.51.0")]
        public partial Sample.MyNumber Add(Sample.MyNumber other);
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Mixin Task", "1.0.51.0")]
        public static Sample.MyNumber operator +(Sample.MyNumber a1, Sample.MyNumber a2)
        {
            return a1.Add(a2);
        }
    }
}

```

As you can see absract methods are implemented as partial Methods. That way you can reqirer the consumer of
your Mixin to provide specific functionallity.



## Restrictions
+ The classes that implement the mixins must be partial. 
+ ~~Mixins may not have constructors.~~
+ Mixins may not have methods with the same method signiture then any other mixin that is
  implemented by the same class, or any method of the implementing class itself. _(unless explicitly
  implemented methods of interfaces)_  
* Mixins should not inhire from anything other than ```Object```
 

## Roadmap
- [ ] Better compiletime error reporting
- [x] Generated source file shuold automaticly added to the Project 
- [x] Automated NuGet build _(ci)_
- [x] Allow mixins of mixins. _(As long it is no circal dependency)_
- [x] Support for Generic Mixins
- [x] Better using conflict resolve strategy
- [x] Add ```GenerteadCodeAttribute``` to Methods and Propertys

## Legal 
This Software is licensed under [MIT](https://tldrlegal.com/license/mit-license#summary).

### Used Assets
Icon *Combine* created by [Paul Philippe Berthelon Bravo](https://thenounproject.com/paulberthelon) published under Public Domain.
