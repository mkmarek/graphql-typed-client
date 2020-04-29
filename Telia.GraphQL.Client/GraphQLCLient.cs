﻿using GraphQLParser;
using Telia.GraphQL.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;
using GraphQLParser.AST;

namespace Telia.GraphQL
{
    public abstract class GraphQLCLient<TQueryType>
    {
        protected readonly INetworkClient client;

        public GraphQLCLient(string endpoint) : this(new DefaultNetworkClient(endpoint))
        {

        }

        public GraphQLCLient(INetworkClient client)
        {
            this.client = client;
        }

        public GraphQLResult<TReturn> Query<TReturn>(Expression<Func<TQueryType, TReturn>> selector)
        {
            var context = new QueryContext();

            var query = this.CreateOperation(selector, context, OperationType.Query);
            var composer = new ResponseComposer<TQueryType, TReturn>(selector, context);
            var result = this.client.Send(query);

            if (string.IsNullOrWhiteSpace(result))
            {
                return new GraphQLResult<TReturn>(default, null);
            }

            var response = JsonConvert.DeserializeObject<GraphQLResult>(result);
            var value = response.Data == null
                ? default
                : composer.Compose(response.Data);

            return new GraphQLResult<TReturn>(value, response.Errors);
        }

        public string CreateQuery<TReturn>(Expression<Func<TQueryType, TReturn>> selector)
        {
            return this.CreateOperation(selector, new QueryContext(), OperationType.Query);
        }

        internal string CreateOperation<TType, TReturn>(
            Expression<Func<TType, TReturn>> selector,
            QueryContext context,
            OperationType operationType)
        {
            var astPrinter = new Printer();

            var grouping = new SelectionChainGrouping(context);
            var converter = new SelectionChainConverter(context);
            var visitor = new PathGatheringVisitor(context);
            var expander = new SelectionChainExpander(context);

            visitor.Visit(selector);
            var expanded = expander.Expand();

            context.SelectionChains.AddRange(expanded);

            var groupedChains = grouping.Group();

            return astPrinter.Print(new GraphQLOperationDefinition
            {
                Operation = operationType,
                SelectionSet = converter.Convert(groupedChains)
            });
        }
    }

    public abstract class GraphQLCLient<TQueryType, TMutationType>: GraphQLCLient<TQueryType>
    {
        public GraphQLCLient(string endpoint) : base(endpoint)
        {
        }

        public GraphQLCLient(INetworkClient client) : base(client)
        {
        }

        public string CreateMutation<TReturn>(Expression<Func<TMutationType, TReturn>> selector)
        {
            return this.CreateOperation(selector, new QueryContext(), OperationType.Mutation);
        }

        public GraphQLResult<TReturn> Mutation<TReturn>(Expression<Func<TMutationType, TReturn>> selector)
        {
            var context = new QueryContext();

            var query = this.CreateOperation(selector, context, OperationType.Mutation);
            var composer = new ResponseComposer<TMutationType, TReturn>(selector, context);
            var result = this.client.Send(query);

            if (string.IsNullOrWhiteSpace(result))
            {
                return new GraphQLResult<TReturn>(default, null);
            }

            var response = JsonConvert.DeserializeObject<GraphQLResult>(result);
            var value = response.Data == null
                ? default
                : composer.Compose(response.Data);

            return new GraphQLResult<TReturn>(value, response.Errors);
        }
    }
}
