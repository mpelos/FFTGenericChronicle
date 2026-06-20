import unittest

from tools import sim_damage


class SimDamageTests(unittest.TestCase):
    def setUp(self):
        self.spec = {
            "version": "test-policy-v0",
            "type_responses": {
                "plate": {
                    "swing": 0.60,
                    "thrust": 0.65,
                    "crush": 1.15,
                    "missile": 0.85,
                },
                "mail": {
                    "swing": 0.75,
                    "thrust": 1.15,
                    "crush": 0.95,
                    "missile": 1.10,
                },
                "leather": {
                    "swing": 0.95,
                    "thrust": 0.95,
                    "crush": 1.00,
                    "missile": 0.95,
                },
                "cloth": {
                    "swing": 1.00,
                    "thrust": 1.00,
                    "crush": 1.00,
                    "missile": 1.00,
                },
            },
            "stacking": {
                "min_total_multiplier": 0.35,
                "max_total_multiplier": 1.75,
            },
            "modifiers": {
                "protect": 0.50,
                "shell": 0.50,
            },
        }

    def scenario(self, armor_class, damage_type, **target_overrides):
        target = {"armor_class": armor_class}
        target.update(target_overrides)
        return {
            "scenario_id": f"{armor_class}-{damage_type}",
            "attacker": {"pa": 10, "ma": 10, "speed": 8, "brave": 70, "faith": 70},
            "target": target,
            "action": {
                "family": "test",
                "routine": "pa_wp",
                "wp": 10,
                "damage_type": damage_type,
                "axis": "physical",
            },
        }

    def test_plate_resists_swing_and_thrust_but_is_weak_to_crush(self):
        swing = sim_damage.calculate_damage(self.scenario("plate", "swing"), self.spec)
        thrust = sim_damage.calculate_damage(self.scenario("plate", "thrust"), self.spec)
        crush = sim_damage.calculate_damage(self.scenario("plate", "crush"), self.spec)

        self.assertLess(swing["expected_damage_on_hit"], swing["base_pressure"])
        self.assertLess(thrust["expected_damage_on_hit"], thrust["base_pressure"])
        self.assertGreater(crush["expected_damage_on_hit"], swing["expected_damage_on_hit"])
        self.assertGreater(crush["expected_damage_on_hit"], thrust["expected_damage_on_hit"])

    def test_mail_is_weaker_to_thrust_than_swing(self):
        swing = sim_damage.calculate_damage(self.scenario("mail", "swing"), self.spec)
        thrust = sim_damage.calculate_damage(self.scenario("mail", "thrust"), self.spec)

        self.assertGreater(thrust["expected_damage_on_hit"], swing["expected_damage_on_hit"])

    def test_total_multiplier_has_defensive_floor_cap(self):
        result = sim_damage.calculate_damage(
            self.scenario(
                "plate",
                "swing",
                protect=True,
                element_multiplier=0.50,
                zodiac_multiplier=0.50,
            ),
            self.spec,
        )

        self.assertAlmostEqual(result["uncapped_total_multiplier"], 0.075)
        self.assertAlmostEqual(result["total_multiplier"], 0.35)
        self.assertEqual(result["expected_damage_on_hit"], 35)

    def test_total_multiplier_has_offensive_ceiling_cap(self):
        scenario = self.scenario(
            "plate",
            "crush",
            element_multiplier=1.50,
            zodiac_multiplier=1.50,
        )
        self.spec["type_responses"]["plate"]["crush"] = 1.50
        result = sim_damage.calculate_damage(scenario, self.spec)

        self.assertAlmostEqual(result["uncapped_total_multiplier"], 3.375)
        self.assertAlmostEqual(result["total_multiplier"], 1.75)
        self.assertEqual(result["expected_damage_on_hit"], 175)

    def test_chip_floor_keeps_nonzero_damage_at_one(self):
        scenario = self.scenario("plate", "swing")
        scenario["attacker"]["pa"] = 1
        scenario["action"]["wp"] = 1
        scenario["target"]["protect"] = True
        scenario["target"]["element_multiplier"] = 0.50
        scenario["target"]["zodiac_multiplier"] = 0.50

        result = sim_damage.calculate_damage(scenario, self.spec)

        self.assertEqual(result["expected_damage_on_hit"], 1)

    def test_random_routine_reports_distribution_after_modifiers(self):
        scenario = self.scenario("cloth", "crush")
        scenario["attacker"]["pa"] = 3
        scenario["action"]["routine"] = "random_pa_wp"

        result = sim_damage.calculate_damage(scenario, self.spec)

        self.assertEqual(result["min"], 10)
        self.assertEqual(result["max"], 30)
        self.assertEqual(result["expected_damage_on_hit"], 20)

    def test_bundle_penetration_softens_response_toward_ceiling(self):
        bundle = {
            "calc": {
                "penetration_ceiling": 1.10,
                "combined_multiplier_clamp": [0.25, 2.5],
                "chip_floor": 1,
                "default_brave": 70,
                "protect_multiplier": 0.667,
            },
            "armor_response": {
                "plate": {"swing": 0.65, "thrust": 0.65, "crush": 1.15, "missile": 0.80}
            },
            "families": {
                "gun": {
                    "routine": "wp_wp",
                    "damage_type": "missile",
                    "wp": 9,
                    "penetration": 0.70,
                }
            },
            "jobs": {
                "Chemist": {"bands": {"late": {"pa": 8, "ma": 8, "spd": 8, "hp": 344}}}
            },
        }

        result = sim_damage.calculate_family_damage(bundle, "gun", "Chemist", "plate", "late")

        self.assertAlmostEqual(result["type_response"], 1.01)
        self.assertEqual(result["base_pressure"], 81)
        self.assertEqual(result["expected_damage_on_hit"], 81)

    def test_bundle_sweep_rows_include_review_fields(self):
        bundle = {
            "version": "sim-inputs-v0",
            "provenance": {"result_class": "conceptually viable"},
            "calc": {
                "penetration_ceiling": 1.10,
                "combined_multiplier_clamp": [0.25, 2.5],
                "chip_floor": 1,
                "default_brave": 70,
                "default_faith": 70,
                "protect_multiplier": 0.667,
                "zodiac": {"neutral": 1.0},
            },
            "armor_response": {
                "plate": {"swing": 0.65, "thrust": 0.65, "crush": 1.15, "missile": 0.80},
                "mail": {"swing": 0.75, "thrust": 1.10, "crush": 0.95, "missile": 1.00},
            },
            "families": {
                "sword": {"routine": "pa_wp", "damage_type": "swing", "wp": 16, "penetration": 0.0}
            },
            "jobs": {
                "Knight": {
                    "armor_class": "plate",
                    "bands": {"late": {"pa": 12, "ma": 8, "spd": 8, "hp": 516}},
                },
                "Squire": {
                    "armor_class": "mail",
                    "bands": {"late": {"pa": 9, "ma": 8, "spd": 8, "hp": 430}},
                },
            },
        }

        rows = sim_damage.build_global_sweep(bundle, phases=["late"], armor_classes=["plate"])

        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["scenario_set_version"], "scenario-set-v0")
        self.assertEqual(rows[0]["attacker_job"], "Knight")
        self.assertEqual(rows[0]["target_armor_class"], "plate")
        self.assertEqual(rows[0]["formula_or_routine"], "pa_wp")
        self.assertEqual(rows[0]["technical_verdict"], "conceptually viable")

    def test_scorecard_reports_plate_matchup(self):
        bundle = {
            "version": "sim-inputs-v0",
            "provenance": {"result_class": "conceptually viable"},
            "calc": {
                "penetration_ceiling": 1.10,
                "combined_multiplier_clamp": [0.25, 2.5],
                "chip_floor": 1,
                "default_brave": 70,
                "default_faith": 70,
                "protect_multiplier": 0.667,
                "zodiac": {"neutral": 1.0},
            },
            "armor_response": {
                "plate": {"swing": 0.65, "thrust": 0.65, "crush": 1.15, "missile": 0.80},
                "mail": {"swing": 0.75, "thrust": 1.10, "crush": 0.95, "missile": 1.00},
            },
            "families": {
                "sword": {"routine": "pa_wp", "damage_type": "swing", "wp": 10, "penetration": 0.0},
                "spear": {"routine": "pa_wp", "damage_type": "thrust", "wp": 10, "penetration": 0.0},
                "axe": {"routine": "pa_wp", "damage_type": "crush", "wp": 10, "penetration": 0.0},
            },
            "jobs": {
                "Knight": {
                    "armor_class": "plate",
                    "bands": {"late": {"pa": 10, "ma": 8, "spd": 8, "hp": 500}},
                },
                "Dragoon": {
                    "armor_class": "plate",
                    "bands": {"late": {"pa": 10, "ma": 8, "spd": 8, "hp": 500}},
                },
            },
        }
        rows = sim_damage.build_global_sweep(bundle, phases=["late"], armor_classes=["plate", "mail"])
        scorecard = sim_damage.build_scorecard(bundle, rows)

        self.assertTrue(scorecard["plate_matchup_observable"]["pass"])
        self.assertIn("axe", scorecard["family_viability"]["viable_families"])

    def test_phase_wp_scalar_applies_to_weapon_power(self):
        bundle = {
            "calc": {
                "penetration_ceiling": 1.10,
                "combined_multiplier_clamp": [0.25, 2.5],
                "chip_floor": 1,
                "default_brave": 70,
                "default_faith": 70,
                "protect_multiplier": 0.667,
                "phase_wp_scalar": {"early": 0.5, "late": 1.0},
            },
            "armor_response": {
                "cloth": {"swing": 1.0, "thrust": 1.0, "crush": 1.0, "missile": 1.0}
            },
            "families": {
                "gun": {
                    "routine": "wp_wp",
                    "damage_type": "missile",
                    "wp": 12,
                    "penetration": 0.0,
                }
            },
            "jobs": {
                "Chemist": {
                    "bands": {
                        "early": {"pa": 4, "ma": 4, "spd": 6, "hp": 120},
                        "late": {"pa": 8, "ma": 8, "spd": 8, "hp": 344},
                    }
                }
            },
        }

        early = sim_damage.calculate_family_damage(bundle, "gun", "Chemist", "cloth", "early")
        late = sim_damage.calculate_family_damage(bundle, "gun", "Chemist", "cloth", "late")

        self.assertEqual(early["base_pressure"], 36)
        self.assertEqual(early["expected_damage_on_hit"], 36)
        self.assertEqual(late["base_pressure"], 144)

    def test_stress_engines_are_read_from_bundle(self):
        bundle = {
            "calc": {
                "penetration_ceiling": 1.10,
                "combined_multiplier_clamp": [0.25, 2.5],
                "chip_floor": 1,
                "default_brave": 70,
                "default_faith": 70,
                "protect_multiplier": 0.667,
            },
            "stress_engines": {"two_hands": 1.8, "two_swords_hits": 2, "attack_boost": 1.25},
            "armor_response": {
                "cloth": {"swing": 1.0, "thrust": 1.0, "crush": 1.0, "missile": 1.0}
            },
            "families": {
                "sword": {"routine": "pa_wp", "damage_type": "swing", "wp": 10, "penetration": 0.0},
            },
            "jobs": {
                "Knight": {
                    "armor_class": "plate",
                    "bands": {"stress": {"pa": 10, "ma": 8, "spd": 8, "hp": 500}},
                },
                "Black Mage": {
                    "armor_class": "cloth",
                    "bands": {"stress": {"pa": 4, "ma": 10, "spd": 8, "hp": 300}},
                },
            },
        }

        rows = sim_damage.build_global_sweep(bundle, phases=["stress"], armor_classes=["cloth"])
        two_hands = next(row for row in rows if row["support_context"] == "two_hands")

        self.assertEqual(two_hands["expected_damage_after_hit_rate"], 180)

    def test_magic_faith_factor_floor_uses_rounding(self):
        bundle = {
            "version": "sim-inputs-v0.2",
            "calc": {
                "combined_multiplier_clamp": [0.25, 2.5],
                "chip_floor": 1,
                "default_faith": 70,
                "zodiac": {"neutral": 1.0},
            },
            "magic": {
                "routine": "K*MA*max(faith_floor,(CFa/100)*(TFa/100))",
                "sample_spells": {"high": 26},
                "shell_multiplier": 0.667,
                "faith_factor_floor": 0.6,
            },
            "jobs": {
                "Black Mage": {
                    "armor_class": "cloth",
                    "bands": {"stress": {"pa": 8, "ma": 18, "spd": 10, "hp": 400}},
                },
                "Knight": {
                    "armor_class": "plate",
                    "bands": {"stress": {"pa": 14, "ma": 10, "spd": 9, "hp": 624}},
                },
            },
        }

        row = sim_damage.build_magic_row(bundle, "stress", "plate")

        self.assertEqual(row["expected_damage_after_hit_rate"], 281)


if __name__ == "__main__":
    unittest.main()
