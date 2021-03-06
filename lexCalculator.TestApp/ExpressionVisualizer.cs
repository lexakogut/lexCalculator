﻿using lexCalculator.Types;
using lexCalculator.Static;
using lexCalculator.Linking;
using lexCalculator.Types.TreeNodes;
using System;

namespace lexCalculator.TestApp
{
	class ExpressionVisualizer
	{
		public void VisualizeAsTree(TreeNode node, CalculationContext context)
		{
			VisualizeAsTreeRecursion(node, context.VariableTable, context.FunctionTable, true, String.Empty);
		}

		public void VisualizeAsTreeRecursion(TreeNode node, 
			IReadOnlyTable<double> variableTable, 
			IReadOnlyTable<FinishedFunction> functionTable, 
			bool recursivelyVisualiseFunctions, 
			string colorStr)
		{
			for (int i = 0; i < colorStr.Length; ++i)
			{
				if (colorStr[i] == '0') Console.ForegroundColor = ConsoleColor.Gray;
				if (colorStr[i] == '1') Console.ForegroundColor = ConsoleColor.Red;
				if (colorStr[i] == '2') Console.ForegroundColor = ConsoleColor.Yellow;
				if (colorStr[i] == '3') Console.ForegroundColor = ConsoleColor.Magenta;
				Console.Write("|   ");
				Console.ResetColor();
			}

			switch (node)
			{
				case NumberTreeNode lTreeNode:
				{
					Console.WriteLine(lTreeNode.Value.ToString("G7", System.Globalization.CultureInfo.InvariantCulture));
				}
				break;

				case UndefinedVariableTreeNode vTreeNode:
				{
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine(vTreeNode.Name);
					Console.ResetColor();
				}
				break;

				case FunctionParameterTreeNode fpTreeNode:
				{
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					Console.WriteLine(String.Format("${0}", fpTreeNode.Index));
					Console.ResetColor();
				}
				break;

				case VariableIndexTreeNode iTreeNode:
				{
					Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine(String.Format("V:{0}", iTreeNode.Index));
					Console.ResetColor();
				}
				break;

				case FunctionIndexTreeNode fiTreeNode:
				{
					if (recursivelyVisualiseFunctions)
					{
						TreeNode clone = functionTable[fiTreeNode.Index].TopNode.Clone();
						DefaultLinker linker = new DefaultLinker();
						clone = linker.ReplaceParametersWithTreeNodes(clone, fiTreeNode.Parameters);
						
						VisualizeAsTreeRecursion(clone, variableTable, functionTable, recursivelyVisualiseFunctions, colorStr);
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine(String.Format("[F:{0}]", fiTreeNode.Index));
						foreach (TreeNode child in fiTreeNode.Parameters)
						{
							VisualizeAsTreeRecursion(child, variableTable, functionTable, recursivelyVisualiseFunctions, colorStr + '3');
						}
						Console.ResetColor();
					}
				}
				break;

				case UndefinedFunctionTreeNode fTreeNode:
				{
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine(String.Format("{0}()", fTreeNode.Name));
					Console.ResetColor();
					foreach (TreeNode child in fTreeNode.Parameters)
					{
						VisualizeAsTreeRecursion(child, variableTable, functionTable, recursivelyVisualiseFunctions, colorStr + '0');
					}
					break;
				}

				case UnaryOperationTreeNode uTreeNode:
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(String.Format("[{0}]", uTreeNode.Operation.FunctionName));
					Console.ResetColor();
					VisualizeAsTreeRecursion(uTreeNode.Child, variableTable, functionTable, recursivelyVisualiseFunctions, colorStr + '1');
					break;
				}

				case BinaryOperationTreeNode bTreeNode:
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine(String.Format("[{0}]", bTreeNode.Operation.FunctionName));
					Console.ResetColor();
					VisualizeAsTreeRecursion(bTreeNode.LeftChild, variableTable, functionTable, recursivelyVisualiseFunctions, colorStr + '2');
					VisualizeAsTreeRecursion(bTreeNode.RightChild, variableTable, functionTable, recursivelyVisualiseFunctions, colorStr + '2');
					break;
				}
			}

			/*
			for (int i = 0; i < level; ++i)
			{
				Console.Write("|   ");
			}
			Console.WriteLine();
			*/
		}

		public void VisualizeAsPrefixEquation(TreeNode node)
		{
			switch (node)
			{
				case NumberTreeNode lTreeNode:
				{
					Console.Write(lTreeNode.Value.ToString("G6", System.Globalization.CultureInfo.InvariantCulture));
					Console.Write(' ');
					break;
				}

				case UndefinedVariableTreeNode vTreeNode:
				{
					Console.Write(vTreeNode.Name);
					Console.Write(' ');
					break;
				}

				case FunctionParameterTreeNode fpTreeNode:
				{
					Console.Write(fpTreeNode);
					Console.Write(' ');
					break;
				}

				case VariableIndexTreeNode iTreeNode:
				{
					Console.Write(iTreeNode);
					Console.Write(' ');
					break;
				}

				case UndefinedFunctionTreeNode fTreeNode:
				{
					Console.Write(fTreeNode.Name);
					Console.Write(' ');
					foreach (TreeNode child in fTreeNode.Parameters)
					{
						VisualizeAsPrefixEquation(child);
					}
					break;
				}

				case UnaryOperationTreeNode uTreeNode:
				{
					Console.Write(uTreeNode.Operation.FunctionName);
					Console.Write(' ');
					VisualizeAsPrefixEquation(uTreeNode.Child);
					break;
				}

				case BinaryOperationTreeNode bTreeNode:
				{
					Console.Write(bTreeNode.Operation.FunctionName);
					Console.Write(' ');
					VisualizeAsPrefixEquation(bTreeNode.LeftChild);
					VisualizeAsPrefixEquation(bTreeNode.RightChild);
					break;
				}
			}
		}

		public void VisualizeAsPostfixEquation(TreeNode node)
		{
			switch (node)
			{
				case NumberTreeNode lTreeNode:
				{
					Console.Write(lTreeNode.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
					Console.Write(' ');
					break;
				}

				case UndefinedVariableTreeNode vTreeNode:
				{
					Console.Write(vTreeNode.Name);
					Console.Write(' ');
					break;
				}

				case FunctionParameterTreeNode fpTreeNode:
				{
					Console.Write(fpTreeNode);
					Console.Write(' ');
					break;
				}

				case VariableIndexTreeNode iTreeNode:
				{
					Console.Write(iTreeNode);
					Console.Write(' ');
					break;
				}

				case UndefinedFunctionTreeNode fTreeNode:
				{
					foreach (TreeNode child in fTreeNode.Parameters)
					{
						VisualizeAsPostfixEquation(child);
					}
					Console.Write(fTreeNode.Name);
					Console.Write(' ');
					break;
				}

				case UnaryOperationTreeNode uTreeNode:
				{
					VisualizeAsPostfixEquation(uTreeNode.Child);
					Console.Write(uTreeNode.Operation.FunctionName);
					Console.Write(' ');
					break;
				}

				case BinaryOperationTreeNode bTreeNode:
				{
					VisualizeAsPostfixEquation(bTreeNode.LeftChild);
					VisualizeAsPostfixEquation(bTreeNode.RightChild);
					Console.Write(bTreeNode.Operation.FunctionName);
					Console.Write(' ');
					break;
				}
			}
		}
	}
}
