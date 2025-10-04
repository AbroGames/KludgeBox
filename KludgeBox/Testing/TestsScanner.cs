using System.Reflection;

namespace KludgeBox.Testing;

public static class TestsScanner
{
    public static IReadOnlyList<TestNode> ScanTestClasses()
    {
        var tests = new List<TestNode>();
        
        var types = typeof(TestNode).Assembly.GetTypes();
        var testTypes = types.Where(type => type.IsAssignableTo(typeof(TestNode)));
        var nonAbstractTestTypes = testTypes.Where(type => !type.IsAbstract);
        
        foreach (var type in nonAbstractTestTypes)
        {
            var test = (TestNode)Activator.CreateInstance(type);
            tests.Add(test);
        }
        
        return tests;
    }

    public static TestContext ScanTestMethods(TestNode testInstance)
    {
        var testType = testInstance.GetType();
        var methods = testType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var testMethods = methods.Where(method => method.GetCustomAttribute<TestAttribute>() is not null);
        var skipAttr = testType.GetCustomAttribute<SkipAttribute>();
        var mustBeSkipped = skipAttr is not null;
        
        var classContext = new TestContext(testInstance.TestName, mustBeSkipped ? TestResult.Skipped : TestResult.NotRan);
        classContext.SkipReason = skipAttr?.Reason ?? classContext.SkipReason;
        
        var tests = testMethods.Select(method => CreateTestRunAction(method, testInstance, classContext)).ToList();
        
        return classContext;
    }

    private static TestContext CreateTestRunAction(MethodInfo method, object instance, TestContext parent)
    {
        var caseAttributes = method.GetCustomAttributes<TestCaseAttribute>().ToList();
        var skipAttr = method.GetCustomAttribute<SkipAttribute>();
        var mustBeSkipped = skipAttr is not null;
        var context = new TestContext(method.Name, mustBeSkipped ? TestResult.Skipped : TestResult.NotRan);
        context.SkipReason = skipAttr?.Reason ?? context.SkipReason;
        
        parent?.AddChild(context);
        
        if (!caseAttributes.Any())
        {
            void TestAction()
            {
                method.Invoke(instance, null);
            }

            context.BindAction(TestAction);
        }
        else
        {
            int id = 0;
            foreach (var testCase in caseAttributes)
            {
                var subContext = new TestContext($"#{id++}", mustBeSkipped ? TestResult.Skipped : TestResult.NotRan);
                var testParams = testCase.TestParams;
                context.AddChild(subContext);
                
                void TestAction()
                {
                    method.Invoke(instance, testParams);
                }
                subContext.BindAction(TestAction);
            }
        }
        
        return context;
    }
}