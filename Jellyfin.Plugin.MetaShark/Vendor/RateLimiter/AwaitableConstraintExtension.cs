namespace RateLimiter
{
    /// <summary>
    /// Provides extension to interface <see cref="IAwaitableConstraint"/>
    /// </summary>
    public static class AwaitableConstraintExtension
    {
        /// <summary>
        /// Compose two awaitable constraint in a new one
        /// </summary>
        /// <param name="awaitableConstraint1"></param>
        /// <param name="awaitableConstraint2"></param>
        /// <returns></returns>
        public static IAwaitableConstraint Compose(this IAwaitableConstraint awaitableConstraint1, IAwaitableConstraint awaitableConstraint2)
        {
            if (awaitableConstraint1 == awaitableConstraint2)
                return awaitableConstraint1;

            return new ComposedAwaitableConstraint(awaitableConstraint1, awaitableConstraint2);
        }
    }
}
