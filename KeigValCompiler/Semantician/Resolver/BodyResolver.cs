using KeigValCompiler.Semantician.Binding;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Binds every body into the bound tree, which each member keeps: every function's, accessor's, constructor's and
 * operator's body, and every field's and property's starting value. The standard library's are bound too, and
 * have to bind cleanly. Constant fields come first, in the order they are declared, so that constants which need
 * each other's values are reported as C# reports them, once, at the first; a constant another needs is worked out
 * when first needed, and only once. */
internal class BodyResolver : IPackResolver
{
    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        BodyBindingContext Bodies = new(context);
        Dictionary<PackSourceFile, int> FileOrder = MemberRelations.GetFileOrder(context.Pack);
        foreach (PackField Constant in MemberRelations.OrderByDeclaration(context.Pack.Fields.Where(
            field => field.HasModifier(PackMemberModifiers.Const)), FileOrder).Cast<PackField>())
        {
            Bodies.BindFieldInitializer(Constant);
        }

        foreach (PackMember Member in context.Pack.Members)
        {
            switch (Member)
            {
                case PackFunction Function when Function.Statements != null:
                    Function.BoundBody = new BodyBinder(Bodies, Function).BindFunctionBody();
                    break;

                case PackField Field:
                    Bodies.BindFieldInitializer(Field);
                    break;

                case PackProperty Property when Property.InitialValue != null:
                    Property.BoundInitialValue = new BodyBinder(Bodies, Property)
                        .BindPropertyInitializer(Property);
                    break;
            }
        }
    }
}
