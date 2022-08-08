import unittest
from main import Bctutorial
from AlgorithmImports import *

class AddTests(unittest.TestCase):
    def test_two_plus_two_four(self):
        self.assertEqual(4, Bctutorial.Add(2,2))

