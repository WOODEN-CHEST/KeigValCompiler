namespace KeigValCompiler;

/* The literal "null", as a PrimitiveValueStatement holds it, kept apart from the string "null", which is
 * text. It has no value of its own, so every one is equal. */
internal record NullConstant;
