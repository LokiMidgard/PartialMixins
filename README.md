[![NuGet](https://img.shields.io/nuget/v/PartialMixin.svg?style=flat-square)](https://www.nuget.org/packages/PartialMixin/)

# PartialMixins
Extends C# with Mixins. The Mixins are simulated using partial classes, instead of ilweaving like other
librarys. The MixinAttribute copys all members of the targeted Mixin to a partial class implementation
of the anotated type. This give you intellisense support on your classes.

## Usage

First add the NuGet [package](https://www.nuget.org/packages/PartialMixin/) to your Project.

**Currently there is a Bug preventing the generated source file to be added to the project.**

_Workaround: Manually add the ```Mixin.g.cs``` in the Properties folder to to your Project. This file will be created after the first build_

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
##Restrictions
+ The classes that implement the mixins must be partial. 
+ Mixins may not have constructors.
+ Mixins may not have methods with the same method signiture then any other mixin that is
implemented by the same class, or any method of the implementing class itself. _(unless explicitly
implemented methods of interfaces)_  
* Mixins should not inhire from anything other than ```Object```

## Pros & Cons

_Pro_
* intellisense support
* better debugging
 
_Con_
* changes will only applyed after build
* problems with refactoring. (if not using interfaces) 

##Roadmap
- [ ] Better compiletime error reporting
- [ ] Generated source file shuold automaticly added to the Project 
- [ ] Automated NuGet build _(ci)_
- [ ] Multithreading _(where posible)_
- [x] Allow mixins of mixins. _(As long it is no circal dependency)_
- [x] Support for Generic Mixins
- [x] Better using conflict resolve strategy
- [x] Add ```GenerteadCodeAttribute``` to Methods and Propertys

##Legal 
This Software is licensed under [MIT](https://tldrlegal.com/license/mit-license#summary).

### Used Assets
Icon *Combine* created by [Paul Philippe Berthelon Bravo](https://thenounproject.com/paulberthelon) published under Public Domain.
