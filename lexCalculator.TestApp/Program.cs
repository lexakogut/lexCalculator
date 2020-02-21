﻿using System;
using System.Collections.Generic;
using lexCalculator.Parsing;
using lexCalculator.Types;
using lexCalculator.Linking;
using lexCalculator.Calculation;
using System.Diagnostics;
using lexCalculator.Processing;

namespace lexCalculator.TestApp
{
	class Program
	{
		static Stopwatch watch = new Stopwatch();
		static Random rand = new Random();
		static ExpressionVisualizer visualizer = new ExpressionVisualizer();

		static ITokenizer tokenizer = new MyTokenizer();
		static IConstructor constructor = new MyConstructor();
		static ILinker linker = new MyLinker(true);
		static IConvertor<PostfixFunction> convertor = new PostfixConvertor();
		static ICalculator<PostfixFunction> postfixCalculator = new PostfixCalculator();
		static ICalculator<FinishedFunction> treeCalculator = new TreeCalculator();
		static IOptimizer optimizer = new MyOptimizer();
		
		static void PrintLibrarySummary(CalculationContext context)
		{
			Console.WriteLine("Functions: ");
			foreach (string name in context.FunctionTable.AllItemNames)
			{
				FinishedFunction function = context.FunctionTable.GetItemWithName(name);
				Console.WriteLine("  {0}({1} argument{3}) = {2}", name, function.ParameterCount, function.TopNode,
					(function.ParameterCount != 1) ? "s" : String.Empty);
			}

			Console.WriteLine("Variables: ");
			foreach (string name in context.VariableTable.AllItemNames)
			{
				double variable = context.VariableTable.GetItemWithName(name);
				Console.WriteLine("  {0} = #[{1}] = {2}", name, context.VariableTable[name], 
					variable.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		static void PrintLibrary(CalculationContext context)
		{
			Console.WriteLine("Variables: ");
			foreach (string name in context.VariableTable.AllItemNames)
			{
				double variable = context.VariableTable.GetItemWithName(name);
				Console.WriteLine("  {0} = {1}", name, variable);
			}
			Console.WriteLine();
			Console.WriteLine("Functions: ");
			foreach (string name in context.FunctionTable.AllItemNames)
			{
				FinishedFunction function = context.FunctionTable.GetItemWithName(name);
				Console.WriteLine(String.Format("  Function \"{0}\"", name));
				Console.WriteLine(function.TopNode);
				Console.WriteLine();
				visualizer.VisualizeAsTree(function.TopNode, context);
				Console.WriteLine();
			}
		}

		static void DefineFunctionInput(UnknownFunctionTreeNode fDefinition, string expressionInput, CalculationContext context)
		{
			string[] parameterNames = new string[fDefinition.Parameters.Length];
			for (int i = 0; i < parameterNames.Length; ++i)
			{
				if (fDefinition.Parameters[i] is UnknownVariableTreeNode vTreeNode)
				{
					parameterNames[i] = vTreeNode.Name;
				}
				else throw new Exception("Invalid function definition");
			}
			Token[] tokens = tokenizer.Tokenize(expressionInput);
			TreeNode tree = constructor.Construct(tokens);
			FinishedFunction function = linker.BuildFunction(tree, context, parameterNames);
			context.FunctionTable.AssignItem(fDefinition.Name, function);
		}

		static void DefineVariableInput(UnknownVariableTreeNode vDefinition, string expressionInput, CalculationContext context)
		{
			Token[] tokens = tokenizer.Tokenize(expressionInput);
			TreeNode tree = constructor.Construct(tokens);
			FinishedFunction function = linker.BuildFunction(tree, context, new string[0]);
			double value = treeCalculator.Calculate(function);

			context.VariableTable.AssignItem(vDefinition.Name, value);
		}

		static void CalculateInput(string expressionInput, CalculationContext context)
		{
			Token[] tokens = tokenizer.Tokenize(expressionInput);
			TreeNode tree = constructor.Construct(tokens);
			FinishedFunction function = linker.BuildFunction(tree, context, new string[0]);
			double value = treeCalculator.Calculate(function);
			
			Console.WriteLine(String.Format(" = {0}", value.ToString("G", System.Globalization.CultureInfo.InvariantCulture)));
		}

		static void FindDifferentialInput(string input, int parameterIndex, CalculationContext context)
		{
			string funcName = input.Substring(4).Trim(' ');
			FinishedFunction function = context.FunctionTable.GetItemWithName(funcName);
			MyDifferentiator differentiator = new MyDifferentiator();
			FinishedFunction df = differentiator.FindDifferential(function, parameterIndex);
			Console.WriteLine(df.TopNode);
			visualizer.VisualizeAsTree(df.TopNode, context);
			FinishedFunction odf = optimizer.OptimizeWithTable(df);
			Console.WriteLine("Optimized: ");
			Console.WriteLine(odf.TopNode);
			visualizer.VisualizeAsTree(odf.TopNode, context);
		}

		static void OptimizeInput(string input, CalculationContext context)
		{
			string funcName = input.Substring(10).Trim(' ');
			FinishedFunction unoptimized = context.FunctionTable.GetItemWithName(funcName);
			FinishedFunction optimized = optimizer.OptimizeWithTable(unoptimized);
			Console.WriteLine("Unoptimized: ");
			Console.WriteLine(unoptimized.TopNode);
			visualizer.VisualizeAsTree(unoptimized.TopNode, context);
			Console.WriteLine("Optimized: ");
			Console.WriteLine(optimized.TopNode);
			visualizer.VisualizeAsTree(optimized.TopNode, context);
		}

		static void TestInput(string input, CalculationContext context)
		{
			string funcName = input.Substring(6).Trim(' ');
			FinishedFunction function = context.FunctionTable.GetItemWithName(funcName);
			PostfixFunction postfixFunction = convertor.Convert(function);
			Console.WriteLine("Testing functionality: ");
			TestCalculator(postfixCalculator, postfixFunction, context.VariableTable, function.ParameterCount, 10, 1, true);
			TestCalculator(treeCalculator, function, context.VariableTable, function.ParameterCount, 10, 1, true);
			Console.WriteLine("Testing speed: ");
			TestCalculator(postfixCalculator, postfixFunction, context.VariableTable, function.ParameterCount, 100000, 100, false);
			TestCalculator(treeCalculator, function, context.VariableTable, function.ParameterCount, 100000, 100, false);
		}

		static void TestCalculator<T>(ICalculator<T> calculator, T function, Table<double> table, int parameters, int iterations, int tests, bool printResults)
		{
			long elapsed = 0;
			for (int t = 0; t < tests; ++t)
			{
				double[][] argsMatrix = new double[iterations][];
				for (int i = 0; i < iterations; i++)
				{
					argsMatrix[i] = new double[parameters];
					for (int p = 0; p < parameters; p++)
					{
						argsMatrix[i][p] = Math.Floor(rand.NextDouble() * 100 - 50.0) / 10;
					}
				}
				watch.Restart();
				double[] results = calculator.CalculateMultiple(function, argsMatrix);
				watch.Stop();
				if (printResults)
				{
					for (int i = 0; i < iterations; i++)
					{
						Console.Write("f(");
						if (parameters > 0) Console.Write("{0}", argsMatrix[i][0].ToString("G5", System.Globalization.CultureInfo.InvariantCulture));
						for (int p = 1; p < parameters; p++)
						{
							Console.Write(", {0}", argsMatrix[i][p].ToString("G5", System.Globalization.CultureInfo.InvariantCulture));
						}
						Console.WriteLine(") = {0}", results[i].ToString("G", System.Globalization.CultureInfo.InvariantCulture));
					}
				}
				elapsed += watch.Elapsed.Ticks;
			}
			Console.WriteLine(String.Format("{0} calculations were done in {1}ms ({2} tests), median time for 1 execution: {3}ms",
				iterations, 
				(elapsed / 10000.0).ToString("G4", System.Globalization.CultureInfo.InvariantCulture), 
				tests, 
				(elapsed / 10000.0 / tests / iterations).ToString("G6", System.Globalization.CultureInfo.InvariantCulture)));
		}

		static void Main(string[] args)
		{
			CalculationContext userContext = new CalculationContext();
			userContext.AssignContext(StandardLibrary.GetContext());
			
			var length2d = linker.BuildFunction(constructor.Construct(tokenizer.Tokenize(
				"sqrt(x^2 + y^2)")), userContext, new string[] {
				"x", "y" });
			userContext.FunctionTable.AssignItem("length2d", length2d);
			userContext.FunctionTable.AssignItem("length3d", linker.BuildFunction(constructor.Construct(tokenizer.Tokenize(
				"sqrt(x^2 + y^2 + z^2)")), userContext, new string[] {
				"x", "y", "z" }));
			userContext.FunctionTable.AssignItem("distance2d", linker.BuildFunction(constructor.Construct(tokenizer.Tokenize(
				"length2d(x2 - x1, y2 - y1)")), userContext, new string[] {
				"x1", "y1", "x2", "y2" }));
			userContext.FunctionTable.AssignItem("distance3d", linker.BuildFunction(constructor.Construct(tokenizer.Tokenize(
				"length3d(x2 - x1, y2 - y1, z2 - z1)")), userContext, new string[] {
				"x1", "y1", "z1", "x2", "y2", "z2" }));

			while (true)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("> ");
				string input = Console.ReadLine();
				Console.ResetColor();

				try
				{
					if (String.IsNullOrEmpty(input)) continue;

					if (input.StartsWith("~"))
					{
						if (input.StartsWith("~help"))
						{
							Console.WriteLine("Try those inputs: ");
							Console.ForegroundColor = ConsoleColor.Gray;
							Console.WriteLine("2 + 2");
							Console.WriteLine("f(x) = e^x");
							Console.WriteLine("myVar = f(4)");
							Console.ResetColor();

							Action<string, string, string[]> printCommand = (name, desc, parameters) =>
							{
								Console.ForegroundColor = ConsoleColor.Cyan;
								Console.Write("~{0}", name);
								Console.ForegroundColor = ConsoleColor.Yellow;
								for (int i = 0; i < parameters.Length; ++i)
								{
									Console.Write(" [{0}]", parameters[i]);
								}
								Console.ResetColor();
								Console.Write(" - ");
								Console.ForegroundColor = ConsoleColor.Gray;
								Console.WriteLine(desc);
								Console.ResetColor();
							};

							Console.WriteLine();
							Console.WriteLine("List of commands: ");
							printCommand("help", "this command", new string[0]);
							printCommand("summary", "prints what functions does your library have", new string[0]);
							printCommand("library", "prints all trees of all functions in your library", new string[0]);
							printCommand("tree", "prints function tree", new string[] { "func" });
							printCommand("test", "test function with random parameters for correctness and effectiveness", new string[] { "func" });
							printCommand("dx", "find differential with respect to 1st parameter", new string[] { "func" });
							printCommand("dy", "find differential with respect to 2nd parameter", new string[] { "func" });
							printCommand("dz", "find differential with respect to 3rd parameter", new string[] { "func" });
							continue;
						}

						if (input.StartsWith("~summary"))
						{
							PrintLibrarySummary(userContext);
							continue;
						}

						if (input.StartsWith("~library"))
						{
							PrintLibrary(userContext);
							continue;
						}

						if (input.StartsWith("~tree "))
						{
							string funcName = input.Substring(6).Trim(' ');
							visualizer.VisualizeAsTree(userContext.FunctionTable.GetItemWithName(funcName).TopNode, userContext);
							continue;
						}

						if (input.StartsWith("~test "))
						{
							TestInput(input, userContext);
							continue;
						}

						if (input.StartsWith("~dx "))
						{
							FindDifferentialInput(input, 0, userContext);
							continue;
						}

						if (input.StartsWith("~dy "))
						{
							FindDifferentialInput(input, 1, userContext);
							continue;
						}

						if (input.StartsWith("~dz "))
						{
							FindDifferentialInput(input, 2, userContext);
							continue;
						}

						if (input.StartsWith("~optimize "))
						{
							OptimizeInput(input, userContext);
							continue;
						}

						throw new Exception("Unknown command");
					}

					string expressionString = input;
					TreeNode firstHalfTree = null;
					int equalsPos = input.IndexOf('=');
					if (equalsPos > 0)
					{
						expressionString = input.Substring(equalsPos + 1);
						string identifierString = input.Substring(0, equalsPos);

						Token[] firstHalfTokens = tokenizer.Tokenize(identifierString);
						firstHalfTree = constructor.Construct(firstHalfTokens);

						switch (firstHalfTree)
						{
							case UnknownFunctionTreeNode fDefinition:
								DefineFunctionInput(fDefinition, expressionString, userContext);
								break;

							case UnknownVariableTreeNode vDefinition:
								DefineVariableInput(vDefinition, expressionString, userContext);
								break;

							default: throw new Exception("Unknown syntax");
						}
					}
					else
					{
						CalculateInput(input, userContext);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(String.Format("Error: {0}", ex.Message));
				}
				Console.WriteLine();
			}
		}
	}
}
