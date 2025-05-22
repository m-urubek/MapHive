using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using CommonAnalyzers;
using CSharpVerifier = CommonAnalyzers.Test.CSharpAnalyzerVerifier<CommonAnalyzers.DateTimeToStringAnalyzer>;

namespace CommonAnalyzers.Test
{
    [TestClass]
    public class DateTimeToStringAnalyzerTests
    {
        [TestMethod]
        public async Task NoArguments_ShouldReportDiagnostic()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        var s = {|#0:DateTime.Now.ToString()|};
    }
}";
            var expected = CSharpVerifier.Diagnostic("MH0001").WithLocation(0);
            await CSharpVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task WithFormat_ShouldNotReportDiagnostic()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        var s = DateTime.Now.ToString(""o"");
    }
}";
            await CSharpVerifier.VerifyAnalyzerAsync(test);
        }
    }
} 