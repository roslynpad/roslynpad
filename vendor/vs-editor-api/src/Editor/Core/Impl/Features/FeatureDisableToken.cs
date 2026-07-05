namespace Microsoft.VisualStudio.Utilities.Features.Implementation
{
    internal class FeatureDisableToken : IFeatureDisableToken
    {
        private readonly FeatureService service;
        private readonly string featureName;
        private readonly IFeatureController controller;

        internal FeatureDisableToken(FeatureService service, string featureName, IFeatureController controller)
        {
            this.service = service;
            this.featureName = featureName;
            this.controller = controller;
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly (no need, as there are no managed resources)

        public void Dispose() => this.service.Restore(this.featureName, this.controller);

#pragma warning restore CA1063 // Implement IDisposable Correctly
    }
}
