using Xunit;
using Moq;
using FluentAssertions;
using Pic_Simulator;
using System.Windows.Controls;

namespace Pic_Simulator.Tests
{
    public class InstructionProcessorTests
    {
        private readonly Mock<IBitOperations> _mockBitOps;
        private readonly InstructionProcessor _processor;

        public InstructionProcessorTests()
        {
            _mockBitOps = new Mock<IBitOperations>();
            _processor = new InstructionProcessor(_mockBitOps.Object);
            Command.startUpRam();
        }

        // ----------- SUBLW Tests --------------

        [Fact] // Test 1
        public void SUBLW_ShouldSubtractFromLiteral_WhenWRegIsZero()
        {
            // Arrange
            Command.wReg = 0x00;
            int literal = 0x10;
            int expected = 0x10;

            // Act
            int result = _processor.SUBLW(literal);

            // Assert
            result.Should().Be(1, "SUBLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "Result should be literal - 0");
        }

        [Fact] // Test 2
        public void SUBLW_ShouldSubtractFromLiteral_WhenWRegIsNonZero()
        {
            // Arrange
            Command.wReg = 0x05;
            int literal = 0x10;
            int expected = 0x0B;

            // Act
            int result = _processor.SUBLW(literal);

            // Assert
            result.Should().Be(1, "SUBLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "Result should be literal - W");
        }

        [Fact] // Test 3
        public void SUBLW_ShouldHandleUnderflow_WhenWRegGreaterThanLiteral()
        {
            // Arrange
            Command.wReg = 0x15;
            int literal = 0x10;
            int expected = 0xFB;

            // Act
            int result = _processor.SUBLW(literal);

            // Assert
            result.Should().Be(1, "SUBLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "Result should wrap around on underflow");
        }

        [Fact] // Test 4
        public void SUBLW_ShouldReturnZero_WhenWRegEqualsLiteral()
        {
            // Arrange
            Command.wReg = 0x42;
            int literal = 0x42;
            int expected = 0x00;

            // Act
            int result = _processor.SUBLW(literal);

            // Assert
            result.Should().Be(1, "SUBLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "Result should be zero when values are equal");
        }

        // ----------- XORLW Tests --------------

        [Fact] // Test 5
        public void XORLW_ShouldReturnLiteral_WhenWRegIsZero()
        {
            // Arrange
            Command.wReg = 0x00;
            int literal = 0xAA;
            int expected = 0xAA;

            // Act
            int result = _processor.XORLW(literal);

            // Assert
            result.Should().Be(1, "XORLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "XOR with 0 should return the literal");
        }

        [Fact] // Test 6
        public void XORLW_ShouldReturnZero_WhenWRegEqualsLiteral()
        {
            // Arrange
            Command.wReg = 0x55;
            int literal = 0x55;
            int expected = 0x00;

            // Act
            int result = _processor.XORLW(literal);

            // Assert
            result.Should().Be(1, "XORLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "XOR of same values should be 0");
        }

        [Fact] // Test 7
        public void XORLW_ShouldToggleBits_WhenWRegIsComplement()
        {
            // Arrange
            Command.wReg = 0x0F;
            int literal = 0xF0;
            int expected = 0xFF;

            // Act
            int result = _processor.XORLW(literal);

            // Assert
            result.Should().Be(1, "XORLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "XOR of complements should be all 1s");
        }

        // ----------- IORLW Tests --------------

        [Fact] // Test 8
        public void IORLW_ShouldReturnLiteral_WhenWRegIsZero()
        {
            // Arrange
            Command.wReg = 0x00;
            int literal = 0x42;
            int expected = 0x42;

            // Act
            int result = _processor.IORLW(literal);

            // Assert
            result.Should().Be(1, "IORLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "OR with 0 should return the literal");
        }

        [Fact] // Test 9
        public void IORLW_ShouldReturnSameValue_WhenLiteralIsZero()
        {
            // Arrange
            Command.wReg = 0x42;
            int literal = 0x00;
            int expected = 0x42;

            // Act
            int result = _processor.IORLW(literal);

            // Assert
            result.Should().Be(1, "IORLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "OR with 0 should return W register value");
        }

        [Fact] // Test 10
        public void IORLW_ShouldSetAllBits_WhenBothAreMaxValue()
        {
            // Arrange
            Command.wReg = 0xFF;
            int literal = 0xFF;
            int expected = 0xFF;

            // Act
            int result = _processor.IORLW(literal);

            // Assert
            result.Should().Be(1, "IORLW should return 1 cycle");
            Command.wReg.Should().Be(expected, "OR of max values should remain max");
        }

        // ----------- CLRWDT Test --------------

        [Fact] // Test 11
        public void CLRWDT_ShouldResetWatchdogTimer_WhenCalled()
        {
            // Arrange
            Command.watchdog = 1000;

            // Act
            int result = _processor.CLRWDT();

            // Assert
            result.Should().Be(1, "CLRWDT should return 1 cycle");
            Command.watchdog.Should().Be(18000, "Watchdog timer should be reset to maximum value");
        }

        // ----------- CLRF Tests --------------

        [Fact] // Test 12
        public void CLRF_ShouldClearMemoryLocation_WhenMemoryHasValue()
        {
            // Arrange
            int address = 0x40;
            Command.ram[Command.bank, address] = 0xFF;

            // Act
            int result = _processor.CLRF(address);

            // Assert
            result.Should().Be(1, "CLRF should return 1 cycle");
            Command.ram[Command.bank, address].Should().Be(0, "Memory location should be cleared to 0");
        }

        [Fact] // Test 13
        public void CLRF_ShouldClearAlreadyZero_WhenMemoryIsZero()
        {
            // Arrange
            int address = 0x45;
            Command.ram[Command.bank, address] = 0x00;

            // Act
            int result = _processor.CLRF(address);

            // Assert
            result.Should().Be(1, "CLRF should return 1 cycle");
            Command.ram[Command.bank, address].Should().Be(0, "Memory location should remain 0");
        }

        //---------- BitOps using Mock ----------

        [Fact] // Test 14
        public void BCF_ShouldCallBitOperationsInterface_WhenCalled()
        {
            // Arrange
            int address = 0x20;
            _mockBitOps.Setup(x => x.BCF(address)).Returns(1);

            // Act
            int result = _processor.BCF(address);

            // Assert
            result.Should().Be(1, "BCF should return the value from BitOperations");
            _mockBitOps.Verify(x => x.BCF(address), Times.Once, "BCF should call the bit operations interface exactly once");
        }

        [Fact] // Test 15 
        public void BSF_ShouldCallBitOperationsInterface_WhenCalled()
        {
            // Arrange
            int address = 0x30;
            _mockBitOps.Setup(x => x.BSF(address)).Returns(1);

            // Act
            int result = _processor.BSF(address);

            // Assert
            result.Should().Be(1, "BSF should return the value from BitOperations");
            _mockBitOps.Verify(x => x.BSF(address), Times.Once, "BSF should call the bit operations interface exactly once");
        }

        [Fact] // Test 16
        public void BTFSC_ShouldCallBitOperationsInterface_WhenCalled()
        {
            // Arrange
            int address = 0x25;
            _mockBitOps.Setup(x => x.BTFSC(address, null)).Returns(2);

            // Act
            int result = _processor.BTFSC(address, null);

            // Assert
            result.Should().Be(2, "BTFSC should return the value from BitOperations");
            _mockBitOps.Verify(x => x.BTFSC(address, null), Times.Once, 
                "BTFSC should call the bit operations interface with the correct parameters");
        }
    }
}