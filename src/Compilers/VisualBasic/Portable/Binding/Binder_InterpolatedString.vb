﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Binder

        Private Function BindInterpolatedStringExpression(syntax As InterpolatedStringExpressionSyntax, diagnostics As DiagnosticBag) As BoundExpression

            Dim contentBuilder = ArrayBuilder(Of BoundNode).GetInstance()

            For Each item In syntax.Contents

                Select Case item.Kind
                    Case SyntaxKind.InterpolatedStringText
                        contentBuilder.Add(BindInterpolatedStringText(DirectCast(item, InterpolatedStringTextSyntax), diagnostics))
                    Case SyntaxKind.Interpolation
                        contentBuilder.Add(BindInterpolation(DirectCast(item, InterpolationSyntax), diagnostics))
                    Case Else
                        Throw ExceptionUtilities.Unreachable
                End Select
            Next

            Return New BoundInterpolatedStringExpression(syntax, contentBuilder.ToImmutableAndFree(), GetSpecialType(SpecialType.System_String, syntax, diagnostics))

        End Function

        Private Function BindInterpolatedStringText(syntax As InterpolatedStringTextSyntax, diagnostics As DiagnosticBag) As BoundLiteral
            Return CreateStringLiteral(syntax, syntax.TextToken.ValueText, compilerGenerated:=False, diagnostics:=diagnostics)
        End Function

        Private Function BindInterpolation(syntax As InterpolationSyntax, diagnostics As DiagnosticBag) As BoundInterpolation

            Dim expression = BindRValue(syntax.Expression, diagnostics)

            Dim alignmentOpt As BoundExpression = Nothing

            If syntax.AlignmentClause IsNot Nothing Then

                alignmentOpt = BindRValue(syntax.AlignmentClause.Value, diagnostics)

                If Not (alignmentOpt.IsConstant AndAlso alignmentOpt.ConstantValueOpt.IsIntegral) AndAlso
                   Not syntax.AlignmentClause.Value.HasErrors _
                Then
                    ReportDiagnostic(diagnostics, syntax.AlignmentClause.Value, ERRID.ERR_ExpectedIntLiteral)
                End If
            End If

            Dim formatStringOpt = If(syntax.FormatClause IsNot Nothing,
                                  CreateStringLiteral(syntax.FormatClause, syntax.FormatClause.FormatStringToken.ValueText, compilerGenerated:=False, diagnostics:=diagnostics),
                                  Nothing)

            Return New BoundInterpolation(syntax, expression, alignmentOpt, formatStringOpt)

        End Function

    End Class
End Namespace
