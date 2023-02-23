﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Quix.Streams.Process.Models;
using Xunit;

namespace Quix.Streams.Streaming.UnitTests.Models
{
    public class StreamParametersConsumerShould
    {

        [Fact]
        public void Receive_TimeseriesData_ShouldRaiseExpectedOnReceivedEvents()
        {
            const int PacketSizeTest = 10;
            const int NumberTimestampsTest = 1000;

            // Arrange
            var streamConsumer = Substitute.For<IStreamConsumerInternal>();
            var receivedData = new List<Streaming.Models.TimeseriesData>();
            var parametersReader = new Streaming.Models.StreamConsumer.StreamTimeseriesConsumer(new TestStreamingClient().GetTopicConsumer(), streamConsumer);

            var buffer = parametersReader.CreateBuffer();
            buffer.OnDataReleased += (sender, args) =>
            {
                receivedData.Add(args.Data);
            };
            buffer.PacketSize = PacketSizeTest;

            //Act
            for (var i = 1; i <= NumberTimestampsTest; i++)
            {
                var timeseriesData = new Streaming.Models.TimeseriesData();
                timeseriesData.AddTimestampNanoseconds(100 * i)
                    .AddValue($"test_numeric_param{i}", i)
                    .AddValue($"test_string_param{i}", $"{i}")
                    .AddTag($"tag{i}", $"{i}");

                streamConsumer.OnTimeseriesData += Raise.Event<Action<IStreamConsumer, TimeseriesDataRaw>>(streamConsumer, timeseriesData.ConvertToProcessData());
            }

            // Assert
            receivedData.Count.Should().Be(NumberTimestampsTest / PacketSizeTest);

            for (var packet = 0; packet < NumberTimestampsTest / PacketSizeTest; packet++)
            {
                for (var i = 1; i <= PacketSizeTest; i++)
                {
                    receivedData[packet].Timestamps[i - 1].TimestampNanoseconds.Should().Be(100 * (i + packet * PacketSizeTest));
                    receivedData[packet].Timestamps[i - 1].Parameters[$"test_numeric_param{i + packet * PacketSizeTest}"].NumericValue.Should().Be(i + packet * PacketSizeTest);
                    receivedData[packet].Timestamps[i - 1].Parameters[$"test_string_param{i + packet * PacketSizeTest}"].StringValue.Should().Be($"{i + packet * PacketSizeTest}");
                    receivedData[packet].Timestamps[i - 1].Tags[$"tag{i + packet * PacketSizeTest}"].Should().Be($"{i + packet * PacketSizeTest}");
                }
            }
        }

        [Fact]
        public void Receive_Definitions_ShouldUpdateDefinitionsProperly()
        {
            // Arrange
            var streamConsumer = Substitute.For<IStreamConsumerInternal>();
            var parametersReader = new Streaming.Models.StreamConsumer.StreamTimeseriesConsumer(new TestStreamingClient().GetTopicConsumer(), streamConsumer);

            var parameterDefinitions = new ParameterDefinitions
            {
                Parameters = new List<ParameterDefinition>()
                {
                    new ParameterDefinition
                    {
                        Id = "Param1",
                        Name = "Parameter One",
                        Description = "The parameter one",
                        Format = "{0}%",
                        Unit = "%",
                        MinimumValue = -10.43,
                        MaximumValue = 100.123,
                        CustomProperties = "custom prop"
                    }
                },
                ParameterGroups = new List<ParameterGroupDefinition>()
                {
                    new ParameterGroupDefinition
                    {
                        Name = "some",
                        Parameters = new List<ParameterDefinition>(),
                        ChildGroups = new List<ParameterGroupDefinition>()
                        {
                            new ParameterGroupDefinition
                            {
                                Name = "nested",
                                Parameters = new List<ParameterDefinition>(),
                                ChildGroups = new List<ParameterGroupDefinition>()
                                {
                                    new ParameterGroupDefinition
                                    {
                                        Name = "group",
                                        Parameters = new List<ParameterDefinition>
                                        {
                                            new ParameterDefinition
                                            {
                                                Id = "param2"
                                            },
                                            new ParameterDefinition
                                            {
                                                Id = "param3"
                                            },
                                            new ParameterDefinition
                                            {
                                                Id = "param4"
                                            }
                                        },
                                        ChildGroups = new List<ParameterGroupDefinition>()
                                    },
                                    new ParameterGroupDefinition
                                    {
                                        Name = "group2",
                                        Parameters = new List<ParameterDefinition>
                                        {
                                            new ParameterDefinition
                                            {
                                                Id = "param5"
                                            },
                                            new ParameterDefinition
                                            {
                                                Id = "param6"
                                            }
                                        },
                                        ChildGroups = new List<ParameterGroupDefinition>
                                        {
                                            new ParameterGroupDefinition
                                            {
                                                Name = "startswithtest",
                                                ChildGroups = new List<ParameterGroupDefinition>(),
                                                Parameters = new List<ParameterDefinition>
                                                {
                                                    new ParameterDefinition
                                                    {
                                                        Id = "param7"
                                                    }
                                                }
                                            }
                                        }
                                    },
                                }
                            }
                        }
                    }
                }
            };

            var expectedDefinitions = new List<Streaming.Models.ParameterDefinition>
            {
                new Streaming.Models.ParameterDefinition
                {
                    Id = "Param1",
                    Name = "Parameter One",
                    Description = "The parameter one",
                    Format = "{0}%",
                    Unit = "%",
                    MinimumValue = -10.43,
                    MaximumValue = 100.123,
                    CustomProperties = "custom prop",
                    Location = ""
                },
                new Streaming.Models.ParameterDefinition
                {
                    Id = "param2",
                    Location = "/some/nested/group"
                },
                new Streaming.Models.ParameterDefinition
                {
                    Id = "param3",
                    Location = "/some/nested/group"

                },
                new Streaming.Models.ParameterDefinition
                {
                    Id = "param4",
                    Location = "/some/nested/group"
                },
                new Streaming.Models.ParameterDefinition
                {
                    Id = "param5",
                    Location = "/some/nested/group2"
                },
                new Streaming.Models.ParameterDefinition
                {
                    Id = "param6",
                    Location = "/some/nested/group2"
                },
                new Streaming.Models.ParameterDefinition
                {
                    Id = "param7",
                    Location = "/some/nested/group2/startswithtest"
                }
            };

            // Act
            streamConsumer.OnParameterDefinitionsChanged += Raise.Event<Action<IStreamConsumer, ParameterDefinitions>>(streamConsumer, parameterDefinitions);

            // Assert
            parametersReader.Definitions.Count.Should().Be(7);
            parametersReader.Definitions.Should().BeEquivalentTo(expectedDefinitions);
        }

    }
}
