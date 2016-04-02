# PartialMixins
Extends C# with Mixins. The Mixins are simulated using partial classes, instead of ilweaving like other
librarys. The MixinAttribute copys all members of the targeted Mixin to a partial class implementation
of the anotated type. This give you intellisense support on your classes.

## Usage

First add the NuGet package to your Project.

**Currently there is a Bug preventing the generated source file to be added to the project.**

_Workaround: Manually add the ```Mixin.g.cs``` in the Properties folder to to your Project. This file will be created after the first build_

To create a Mixin just declare a class.

```
    class IdMixin
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
```

To apply the mixin you need to declare the MixinAttribute on the class that should implement the desired
mixin. This class must also have the ```partial``` modifyer. Pass the Type of the Mixin in the attribute
constructor. After the next time you build your source the mixin is implemented by your class. 

```
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

```
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

## Pros & Cons

_Pro_
* intellisense support
* better debugging
 
_Con_
* changes will only applyed after build
* problems with refactoring. (if not using interfaces) 

##Roadmap
* Better compiletime error reporting
* Generated source file shuold automaticly added to the Project 

## Used Assets
Icon *Combine* created by [Paul Philippe Berthelon Bravo](https://thenounproject.com/paulberthelon) published under Public Domain.