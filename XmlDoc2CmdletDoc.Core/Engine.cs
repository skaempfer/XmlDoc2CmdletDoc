﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Jolt;
using XmlDoc2CmdletDoc.Core.Domain;

namespace XmlDoc2CmdletDoc.Core
{
    public class Engine
    {
        private readonly XNamespace mshNs = XNamespace.Get("http://msh");
        private readonly XNamespace mamlNs = XNamespace.Get("http://schemas.microsoft.com/maml/2004/10");
        private readonly XNamespace commandNs = XNamespace.Get("http://schemas.microsoft.com/maml/dev/command/2004/10");
        private readonly XNamespace devNs = XNamespace.Get("http://schemas.microsoft.com/maml/dev/2004/10");

        public EngineErrorCode GenerateHelp(Options options)
        {
            try
            {
                var assembly = LoadAssembly(options);
                var commentReader = LoadComments(options);
                var cmdletTypes = GetCommands(assembly);

                var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
                                             GenerateHelpItemsElement(cmdletTypes));

                using (var stream = new FileStream(options.OutputHelpFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    document.Save(writer);
                }

                return EngineErrorCode.Success;
            }
            catch (EngineException exception)
            {
                Console.Error.WriteLine(exception);
                return exception.ErrorCode;
            }
        }

        private Assembly LoadAssembly(Options options)
        {
            var assemblyPath = options.AssemblyPath;
            if (!File.Exists(assemblyPath))
            {
                throw new EngineException(EngineErrorCode.AssemblyNotFound,
                                          "Assembly file not found: " + assemblyPath);
            }
            try
            {
                var assemblyDir = Path.GetDirectoryName(assemblyPath);
                AppDomain.CurrentDomain.AssemblyResolve += // TODO: Really ought to track this handler and cleanly remove it.
                    (sender, args) =>
                    {
                        var name = args.Name;
                        var i = name.IndexOf(',');
                        if (i != -1)
                        {
                            name = name.Substring(0, i);
                        }
                        name += ".dll";
                        var path = Path.Combine(assemblyDir, name);
                        return Assembly.LoadFrom(path);
                    };

                return Assembly.LoadFile(assemblyPath);
            }
            catch (Exception exception)
            {
                throw new EngineException(EngineErrorCode.AssemblyLoadError,
                                          "Failed to load assembly from file: " + assemblyPath,
                                          exception);
            }
        }

        private XmlDocCommentReader LoadComments(Options options)
        {
            var docCommentsPath = options.DocCommentsPath;
            if (!File.Exists(docCommentsPath))
            {
                throw new EngineException(EngineErrorCode.AssemblyCommentsNotFound,
                                          "Assembly comments file not found: " + docCommentsPath);
            }
            try
            {
                return new XmlDocCommentReader(docCommentsPath);
            }
            catch (Exception exception)
            {
                throw new EngineException(EngineErrorCode.DocCommentsLoadError,
                                          "Failed to load XML Doc comments ffrom file: " + docCommentsPath,
                                          exception);
            }
        }

        private static IEnumerable<Command> GetCommands(Assembly assembly)
        {
            return assembly.GetTypes()
                           .Where(type => type.IsPublic &&
                               typeof(Cmdlet).IsAssignableFrom(type) &&
                               type.GetCustomAttribute<CmdletAttribute>() != null)
                           .Select(type => new Command(type))
                           .OrderBy(command => command.Noun)
                           .ThenBy(command => command.Verb);
        }

        /// <summary>
        /// Generates the root-level <em>&lt;helpItems&gt;</em> element.
        /// </summary>
        /// <param name="commands">All of the commands in the module being documented.</param>
        /// <returns>The root-level <em>helpItems</em> element.</returns>
        private XElement GenerateHelpItemsElement(IEnumerable<Command> commands)
        {
            var helpItemsElement = new XElement(mshNs + "helpItems", new XAttribute("schema", "maml"));
            foreach (var command in commands)
            {
                helpItemsElement.Add(GenerateCommandElement(command));
            }
            return helpItemsElement;
        }

        /// <summary>
        /// Generates a <em>&lt;command:command&gt;</em> element for the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:command&gt;</em> element that represents the <paramref name="command"/>.</returns>
        private XElement GenerateCommandElement(Command command)
        {
            return new XElement(commandNs + "command",
                                new XAttribute(XNamespace.Xmlns + "maml", mamlNs),
                                new XAttribute(XNamespace.Xmlns + "command", commandNs),
                                new XAttribute(XNamespace.Xmlns + "dev", devNs),
                                GenerateDetailsElement(command),
                                GenerateDescriptionElement(command),
                                GenerateSyntaxElement(command),
                                GenerateParametersElement(command),
                                GenerateInputTypesElement(command),
                                GenerateReturnValuesElement(command),
                                GenerateAlertSetElement(command),
                                GenerateExamplesElement(command),
                                GenerateRelatedLinksElement(command));
        }

        /// <summary>
        /// Generates the <em>&lt;command:details&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:details&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateDetailsElement(Command command)
        {
            return new XElement(commandNs + "details",
                                new XElement(commandNs + "name", command.Name),
                                new XElement(commandNs + "verb", command.Verb),
                                new XElement(commandNs + "noun", command.Noun),
                                new XElement(mamlNs + "description",
                                             new XElement(mamlNs + "para",
                                                          "TODO: Insert the SYNOPSIS text here.")));
        }

        /// <summary>
        /// Generates the <em>&lt;maml:description&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;maml:description&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateDescriptionElement(Command command)
        {
            return new XElement(mamlNs + "description",
                                new XElement(mamlNs + "para",
                                             "TODO: Insert the DESCRIPTION here."));
        }

        /// <summary>
        /// Generates the <em>&lt;command:syntax&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:syntax&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateSyntaxElement(Command command)
        {
            var syntaxElement = new XElement(commandNs + "syntax", "TODO: Insert syntaxItem elements here.");
            return syntaxElement;
        }

        /// <summary>
        /// Generates the <em>&lt;command:parameters&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:parameters&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateParametersElement(Command command)
        {
            var parametersElement = new XElement(commandNs + "parameters");
            foreach (var parameter in command.Parameters)
            {
                parametersElement.Add(GenerateParameterElement(parameter));
            }
            return parametersElement;
        }

        private XElement GenerateParameterElement(Parameter parameter)
        {
            var parameterElement = new XElement(commandNs + "parameter",
                                                        new XAttribute("required", parameter.IsRequired),
                                                        new XAttribute("globbing", parameter.SupportsGlobbing),
                                                        new XAttribute("pipelineInput", parameter.IsPipeline),
                                                        new XAttribute("position", parameter.Position.HasValue ? parameter.Position.ToString() : "named"),
                                                        new XElement(mamlNs + "name", parameter.Name),
                                                        new XElement(mamlNs + "description",
                                                                     new XElement(mamlNs + "para", "TODO: Insert parameter description here.")),
                                                        GenerateTypeElement(parameter.ParameterType));
            var defaultValue = parameter.DefaultValue; // TODO: Get the default value from the doc comments?
            if (defaultValue != null)
            {
                parameterElement.Add(new XElement(devNs + "defaultValue", defaultValue.ToString()));
            }
            return parameterElement;
        }

        /// <summary>
        /// Generates the <em>&lt;command:inputTypes&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:inputTypes&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateInputTypesElement(Command command)
        {
            var parametersElement = new XElement(commandNs + "inputTypes", "TODO: Insert inputType elements here.");
            return parametersElement;
        }

        /// <summary>
        /// Generates the <em>&lt;command:returnValues&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:returnValues&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateReturnValuesElement(Command command)
        {
            var returnValueElement = new XElement(commandNs + "returnValues");
            foreach (var type in command.OutputTypes)
            {
                returnValueElement.Add(new XElement(commandNs + "returnValue",
                                                    GenerateTypeElement(type),
                                                    new XElement(mamlNs + "description"))); // TODO: Attach a brief description to the output type.
            }
            return returnValueElement;
        }

        /// <summary>
        /// Generates the <em>&lt;maml:alertSet&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;maml:alertSet&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateAlertSetElement(Command command)
        {
            var alertSetElement = new XElement(mamlNs + "alertSet", "TODO: Insert title and alert elements here.");
            return alertSetElement;
        }

        /// <summary>
        /// Generates the <em>&lt;command:examples&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;command:examples&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateExamplesElement(Command command)
        {
            var examplesElement = new XElement(commandNs + "examples", "TODO: Insert example elements here.");
            return examplesElement;
        }

        /// <summary>
        /// Generates the <em>&lt;maml:relatedLinks&gt;</em> element for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A <em>&lt;maml:relatedLinks&gt;</em> element for the <paramref name="command"/>.</returns>
        private XElement GenerateRelatedLinksElement(Command command)
        {
            var relatedLinksElement = new XElement(mamlNs + "relatedLinks", "TODO: Insert navigationLink elements here.");
            return relatedLinksElement;
        }

        /// <summary>
        /// Generates a <em>&lt;dev:type&gt;</em> element for a type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private XElement GenerateTypeElement(Type type)
        {
            return new XElement(devNs + "type",
                                new XElement(mamlNs + "name", type.FullName),
                                new XElement(mamlNs + "uri"),
                                new XElement(mamlNs + "description"));
        }
    }
}